using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Models;
using Domain.Entities.Attendance;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Attendance.Get;

internal sealed class GetAttendanceQueryHandler(
    IApplicationDbContext context,
    IHasPermission hasPermission,
    IUserContext userContext)
    : IQueryHandler<GetAttendanceQuery, PaginatedResponse<AttendanceResponse>>
{
    public async Task<Result<PaginatedResponse<AttendanceResponse>>> Handle(GetAttendanceQuery query, CancellationToken cancellationToken)
    {
        IQueryable<Domain.Entities.Attendance.Attendance> attendanceQuery = context.Attendances
        .Where(a => a.CheckInTime.HasValue || a.CheckOutTime.HasValue)
            .Include(a => a.Employee)
            .ThenInclude(e => e.OrganizationalUnit)
            .Include(a => a.Shift)
            .Include(a => a.AttendanceSchedule);

        // check if user role not Admin then apply accessible unit ids filter
        UserInfoDto user = await userContext.GetUserAsync();
        if (user.Role != Role.Admin)
        {
            IEnumerable<Guid> accessibleUnitIds = await hasPermission.GetAccessibleUnitIdsAsync(cancellationToken);
            attendanceQuery = attendanceQuery.Where(a => accessibleUnitIds.Contains(a.Employee.OrganizationalUnitId!.Value));
        }

        // Apply filters
        if (query.EmployeeId.HasValue)
        {
            attendanceQuery = attendanceQuery.Where(a => a.EmployeeId == query.EmployeeId);
        }

        if (query.OrganizationId.HasValue)
        {
            attendanceQuery = attendanceQuery.Where(a => a.OrganizationId == query.OrganizationId);
        }

        if (query.Date.HasValue)
        {
            DateOnly dateFilter = query.Date.Value;
            attendanceQuery = attendanceQuery.Where(a => DateOnly.FromDateTime(a.Date) == dateFilter);
        }

        if (query.Status.HasValue)
        {
            attendanceQuery = attendanceQuery.Where(a => a.Status == query.Status);
        }

        if (query.ShiftId.HasValue)
        {
            attendanceQuery = attendanceQuery.Where(a => a.ShiftId == query.ShiftId);
        }

        // Apply search
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            attendanceQuery = attendanceQuery.Where(a =>
                EF.Functions.Like(a.Employee.FullName, $"%{query.SearchTerm}%") ||
                EF.Functions.Like(a.Employee.Code, $"%{query.SearchTerm}%"));
        }

        // Apply sorting
        if (!string.IsNullOrWhiteSpace(query.SortBy))
        {
            string? sortOrder = query.SortOrder?.ToUpperInvariant();
            bool isDescending = sortOrder == "DESC";

            // NOTE: compare against UPPERCASE labels — ToUpperInvariant() is required by the
            // analyzer (CA1308). Previously the labels were lowercase, so no branch ever
            // matched and every sort silently fell back to date-desc.
            attendanceQuery = query.SortBy.ToUpperInvariant() switch
            {
                "DATE" => isDescending ? attendanceQuery.OrderByDescending(a => a.Date) : attendanceQuery.OrderBy(a => a.Date),
                // Order by check-in precedence (earliest first), pushing rows without a check-in to the end
                // in both directions, with a stable tiebreaker so paging is deterministic.
                "CHECKINTIME" => (isDescending
                        ? attendanceQuery.OrderBy(a => a.CheckInTime.HasValue ? 0 : 1).ThenByDescending(a => a.CheckInTime)
                        : attendanceQuery.OrderBy(a => a.CheckInTime.HasValue ? 0 : 1).ThenBy(a => a.CheckInTime))
                    .ThenBy(a => a.Id),
                "CHECKOUTTIME" => isDescending ? attendanceQuery.OrderByDescending(a => a.CheckOutTime) : attendanceQuery.OrderBy(a => a.CheckOutTime),
                "STATUS" => isDescending ? attendanceQuery.OrderByDescending(a => a.Status) : attendanceQuery.OrderBy(a => a.Status),
                "EMPLOYEENAME" => isDescending ? attendanceQuery.OrderByDescending(a => a.Employee.FirstName) : attendanceQuery.OrderBy(a => a.Employee.FirstName),
                "CREATEDAT" => isDescending ? attendanceQuery.OrderByDescending(a => a.CreatedAt) : attendanceQuery.OrderBy(a => a.CreatedAt),
                _ => attendanceQuery.OrderByDescending(a => a.Date)
            };
        }
        else
        {
            attendanceQuery = attendanceQuery.OrderByDescending(a => a.Date);
        }

        // Get total count
        int totalCount = await attendanceQuery.CountAsync(cancellationToken);

        // Apply pagination and get data
        List<Domain.Entities.Attendance.Attendance> attendanceList = await attendanceQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        // Early return if no attendance records found
        if (attendanceList.Count == 0)
        {
            return PaginatedResponse<AttendanceResponse>.Create(
                new List<AttendanceResponse>(),
                totalCount,
                query.Page,
                query.PageSize);
        }

        // Extract employee IDs and dates for efficient leave querying
        var employeeIds = attendanceList.Select(a => a.EmployeeId).ToHashSet();
        var attendanceDates = attendanceList.Select(a => a.Date.Date).ToHashSet();
        DateTime minDate = attendanceDates.Min();
        DateTime maxDate = attendanceDates.Max();

        // Get approved leaves that cover the attendance dates
        List<Leave> relevantLeaves = await context.Leaves
            .Where(l =>
                employeeIds.Contains(l.EmployeeId) &&
                l.StartDate.Date <= maxDate &&
                l.EndDate.Date >= minDate)
            .ToListAsync(cancellationToken);

        // Create a dictionary for quick lookup: (EmployeeId, Date) -> Leave
        var leaveLookup = relevantLeaves
            .SelectMany(l =>
                Enumerable.Range(0, (l.EndDate.Date - l.StartDate.Date).Days + 1)
                    .Select(offset => l.StartDate.Date.AddDays(offset))
                    .Select(date => new { Date = date, Leave = l }))
            .Where(x => attendanceDates.Contains(x.Date))
            .GroupBy(x => (x.Leave.EmployeeId, x.Date))
            .ToDictionary(g => g.Key, g => g.First().Leave);

        // Map to response DTOs
        var attendances = attendanceList.Select(a =>
        {
            string? excludedDatesString = null;

            // Get ExcludedDates from AttendanceSchedule if exists
            if (a.AttendanceSchedule is not null && a.AttendanceSchedule.ExcludedDates.Count > 0)
            {
                excludedDatesString = string.Join(",", a.AttendanceSchedule.ExcludedDates
                    .Select(d => d.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)));
            }

            // Get Leave information if exists
            leaveLookup.TryGetValue((a.EmployeeId, a.Date.Date), out Leave? leave);

            return new AttendanceResponse
            {
                Id = a.Id,
                EmployeeId = a.EmployeeId,
                OrganizationId = a.OrganizationId,
                OrganizationalName = a.Employee.OrganizationalUnit?.UnitName,
                Date = a.Date,
                CheckInTime = a.CheckInTime,
                CheckOutTime = a.CheckOutTime,
                Status = a.Status,
                ShiftId = a.ShiftId,
                WorkingMinutes = a.WorkingMinutes,
                BreakMinutes = a.BreakMinutes,
                OvertimeMinutes = a.OvertimeMinutes,
                LateMinutes = a.LateMinutes,
                EarlyLeaveMinutes = a.EarlyLeaveMinutes,
                Notes = a.Notes,
                CheckInMethod = a.CheckInMethod,
                CheckOutMethod = a.CheckOutMethod,
                ApprovedBy = a.ApprovedBy,
                ApprovedAt = a.ApprovedAt,
                AttendanceScheduleId = a.AttendanceScheduleId,
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.LastUpdatedAt,
                ExcludedDates = excludedDatesString,
                LeaveType = leave?.LeaveType ?? default,
                LeaveTypeName = leave is not null ? GetDisplayName(leave.LeaveType) : string.Empty,
                LeaveId = leave?.Id ?? Guid.Empty,
                FullName = a.Employee.FullName,
                Code = a.Employee.Code,
                ShiftName = a.Shift?.Name,
                ApproverName = null // Would need to join with Users table for approver name
            };
        }).ToList();

        return PaginatedResponse<AttendanceResponse>.Create(
            attendances,
            totalCount,
            query.Page,
            query.PageSize);
    }

    private static string GetDisplayName(LeaveType leaveType)
    {
        FieldInfo? field = leaveType.GetType().GetField(leaveType.ToString());
        DisplayAttribute? displayAttribute = field?.GetCustomAttribute<DisplayAttribute>();
        return displayAttribute?.Name ?? leaveType.ToString();
    }
}
