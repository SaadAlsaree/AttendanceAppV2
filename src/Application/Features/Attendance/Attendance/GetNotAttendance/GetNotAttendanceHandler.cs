using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Models;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Attendance.Attendance.GetNotAttendance;

internal sealed class GetNotAttendanceHandler(
    IApplicationDbContext context,
    IHasPermission hasPermission,
    IUserContext userContext)
    : IQueryHandler<GetNotAttendanceQuery, PaginatedResponse<GetNotAttendanceResponse>>
{
    public async Task<Result<PaginatedResponse<GetNotAttendanceResponse>>> Handle(GetNotAttendanceQuery query, CancellationToken cancellationToken)
    {
        // Base query: attendance records where there is no check-in and no check-out (absence)
        // Filter: EmpID is not null
        // Exclude: records with AttendanceSchedule.Exceptions for that date
        // Exclude: records where employee has an Approved Leave covering that date
        IQueryable<Domain.Entities.Attendance.Attendance> attendanceQuery = context.Attendances
            .Where(a =>
                !a.CheckInTime.HasValue &&
                !a.CheckOutTime.HasValue &&
                a.Employee.EmpID != null)
            // Exclude if there's a ScheduleIssue (Exception) for this date in the AttendanceSchedule
            .Where(a => a.AttendanceSchedule != null && a.AttendanceSchedule.ExcludedDates.Any())
            .Where(a => a.Employee.Leaves.Any(l =>  l.StartDate <= a.Date && l.EndDate >= a.Date))
            .Include(a => a.Employee)
                .ThenInclude(e => e.OrganizationalUnit)
            .Include(a => a.Shift)
            .Include(a => a.AttendanceSchedule)
            .AsNoTracking();

        // Apply permission filter for non-admin users
        UserInfoDto user = await userContext.GetUserAsync();
        if (user.Role != Role.Admin)
        {
            IEnumerable<Guid> accessibleUnitIds = await hasPermission.GetAccessibleUnitIdsAsync(cancellationToken);
            attendanceQuery = attendanceQuery.Where(a => a.Employee.OrganizationalUnitId.HasValue && accessibleUnitIds.Contains(a.Employee.OrganizationalUnitId.Value));
        }

        // Apply additional filters from query
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
            DateTime dateFilter = query.Date.Value.Date;
            attendanceQuery = attendanceQuery.Where(a => a.Date == dateFilter);
        }

        if (query.Status.HasValue)
        {
            attendanceQuery = attendanceQuery.Where(a => a.Status == query.Status);
        }

        if (query.ShiftId.HasValue)
        {
            attendanceQuery = attendanceQuery.Where(a => a.ShiftId == query.ShiftId);
        }

        // Search term (employee name or code)
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            attendanceQuery = attendanceQuery.Where(a =>
                EF.Functions.Like(a.Employee.FullName, $"%{query.SearchTerm}%") ||
                EF.Functions.Like(a.Employee.Code, $"%{query.SearchTerm}%"));
        }

        // Sorting
        if (!string.IsNullOrWhiteSpace(query.SortBy))
        {
            string? sortOrder = query.SortOrder?.ToUpperInvariant();
            bool isDescending = sortOrder == "DESC";
            attendanceQuery = query.SortBy.ToUpperInvariant() switch
            {
                "date" => isDescending ? attendanceQuery.OrderByDescending(a => a.Date) : attendanceQuery.OrderBy(a => a.Date),
                "status" => isDescending ? attendanceQuery.OrderByDescending(a => a.Status) : attendanceQuery.OrderBy(a => a.Status),
                "employeename" => isDescending ? attendanceQuery.OrderByDescending(a => a.Employee.FirstName) : attendanceQuery.OrderBy(a => a.Employee.FirstName),
                "createdat" => isDescending ? attendanceQuery.OrderByDescending(a => a.CreatedAt) : attendanceQuery.OrderBy(a => a.CreatedAt),
                _ => attendanceQuery.OrderByDescending(a => a.Date)
            };
        }
        else
        {
            attendanceQuery = attendanceQuery.OrderByDescending(a => a.Date);
        }

        // Total count for pagination
        int totalCount = await attendanceQuery.CountAsync(cancellationToken);

        // Retrieve page data
        List<Domain.Entities.Attendance.Attendance> attendanceList = await attendanceQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        // Get approved leaves that cover the attendance dates
        List<Domain.Entities.Attendance.Leave> relevantLeaves = await context.Leaves
            .Where(l => l.Status == LeaveStatus.Approved &&
                attendanceList.Any(a =>
                    a.EmployeeId == l.EmployeeId &&
                    a.Date.Date >= l.StartDate.Date &&
                    a.Date.Date <= l.EndDate.Date))
            .ToListAsync(cancellationToken);

        // Create a dictionary for quick lookup: (EmployeeId, Date) -> Leave
        var attendanceKeys = attendanceList
            .Select(a => (a.EmployeeId, a.Date.Date))
            .ToHashSet();
        var leaveLookup = relevantLeaves
            .SelectMany(l =>
                Enumerable.Range(0, (l.EndDate.Date - l.StartDate.Date).Days + 1)
                    .Select(offset => l.StartDate.Date.AddDays(offset))
                    .Select(date => new { Date = date, Leave = l }))
            .Where(x => attendanceKeys.Contains((x.Leave.EmployeeId, x.Date)))
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
            leaveLookup.TryGetValue((a.EmployeeId, a.Date.Date), out Domain.Entities.Attendance.Leave? leave);

            return new GetNotAttendanceResponse
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
                LeaveId = leave?.Id ?? Guid.Empty,
                // Additional navigation properties
                FullName = a.Employee.FullName,
                Code = a.Employee.Code,
                ShiftName = a.Shift?.Name,
                ApproverName = null // Placeholder – could be joined with Users table
            };
        }).ToList();

        return PaginatedResponse<GetNotAttendanceResponse>.Create(attendances, totalCount, query.Page, query.PageSize);
    }
}

