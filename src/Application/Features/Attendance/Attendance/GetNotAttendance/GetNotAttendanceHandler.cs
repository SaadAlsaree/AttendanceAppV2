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
        // Note: ExcludedDates and Leaves filtering will be done in memory after loading due to EF Core translation limitations
        IQueryable<Domain.Entities.Attendance.Attendance> attendanceQuery = context.Attendances
            .Include(a => a.Employee)
                .ThenInclude(e => e.OrganizationalUnit)
            .Include(a => a.Shift)
            .Include(a => a.AttendanceSchedule)
            .Where(a =>
                a.ShiftId != null &&
                !a.CheckInTime.HasValue &&
                !a.CheckOutTime.HasValue)
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

        DateOnly dateFilter = query.Date ?? DateOnly.FromDateTime(DateTime.Now);
        attendanceQuery = attendanceQuery.Where(a => DateOnly.FromDateTime(a.Date) == dateFilter);

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

        // Load all matching records (before ExcludedDates and Leaves filter)
        // Note: We need to load all to filter ExcludedDates and Leaves in memory due to EF Core translation limitations
        List<Domain.Entities.Attendance.Attendance> allAttendances = await attendanceQuery.ToListAsync(cancellationToken);

        // If no records found, return empty result
        if (allAttendances.Count == 0)
        {
            return PaginatedResponse<GetNotAttendanceResponse>.Create(
                new List<GetNotAttendanceResponse>(),
                0,
                query.Page,
                query.PageSize);
        }

        // Get all approved leaves that might cover any of the attendance dates
        // Extract unique employee IDs and date range from attendance records
        var employeeIds = allAttendances.Select(a => a.EmployeeId).Distinct().ToList();
        DateTime minDate = allAttendances.Min(a => a.Date.Date);
        DateTime maxDate = allAttendances.Max(a => a.Date.Date);

        // Query approved leaves that could potentially cover any attendance date
        List<Domain.Entities.Attendance.Leave> approvedLeaves = employeeIds.Count > 0
            ? await context.Leaves
                .Where(l =>
                    employeeIds.Contains(l.EmployeeId) &&
                    l.Status == LeaveStatus.Approved &&
                    l.StartDate.Date <= maxDate &&
                    l.EndDate.Date >= minDate)
                .ToListAsync(cancellationToken)
            : new List<Domain.Entities.Attendance.Leave>();

        // Create a lookup for quick checking if a date is covered by an approved leave
        var leaveCoverageLookup = approvedLeaves
            .SelectMany(l => Enumerable.Range(0, (l.EndDate.Date - l.StartDate.Date).Days + 1)
                .Select(offset => l.StartDate.Date.AddDays(offset))
                .Select(date => (l.EmployeeId, Date: date)))
            .ToHashSet();

        // Filter to EXCLUDE records where:
        // 1. The date is in ExcludedDates
        // 2. There's an approved leave covering this date
        // Query active attendance schedules for the relevant employees and date range
        var minDateOnly = DateOnly.FromDateTime(minDate);
        var maxDateOnly = DateOnly.FromDateTime(maxDate);

        List<AttendanceSchedule> activeSchedules = employeeIds.Count > 0
            ? await context.AttendanceSchedules
                .Where(s =>
                    employeeIds.Contains(s.EmployeeId) &&
                    s.IsActive &&
                    !s.IsDeleted &&
                    s.StartDate <= maxDateOnly &&
                    (s.EndDate == null || s.EndDate >= minDateOnly))
                .ToListAsync(cancellationToken)
            : new List<AttendanceSchedule>();

        // Filter to EXCLUDE records where:
        // 1. The date is in ExcludedDates (checked against ALL active schedules for that employee)
        // 2. There's an approved leave covering this date
        var filteredAttendances = allAttendances
            .Where(a =>
            {
                var attendanceDate = DateOnly.FromDateTime(a.Date);

                // Exclude if there's an approved leave covering this date
                if (leaveCoverageLookup.Contains((a.EmployeeId, a.Date.Date)))
                {
                    return false;
                }

                // Exclude if the date is in ExcludedDates of ANY active schedule for this employee
                // We check all active schedules because the attendance record might not be linked to the correct schedule
                // or the link might be missing.
                bool isExcluded = activeSchedules.Any(s =>
                    s.EmployeeId == a.EmployeeId &&
                    s.StartDate <= attendanceDate &&
                    (s.EndDate == null || s.EndDate >= attendanceDate) &&
                    s.ExcludedDates.Contains(attendanceDate));

                if (isExcluded)
                {
                    return false;
                }

                // Include all other records (no leave and not in ExcludedDates)
                return true;
            })
            .ToList();

        // Total count after ExcludedDates filter
        int totalCount = filteredAttendances.Count;

        // Apply pagination
        var attendanceList = filteredAttendances
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        // Create a dictionary for quick lookup: (EmployeeId, Date) -> Leave
        // Reuse the approvedLeaves we already loaded, filtered to only those covering the paginated attendance list
        var attendanceKeys = attendanceList
            .Select(a => (a.EmployeeId, a.Date.Date))
            .ToHashSet();
        var leaveLookup = approvedLeaves
            .Where(l => attendanceKeys.Any(ak =>
                ak.EmployeeId == l.EmployeeId &&
                ak.Date >= l.StartDate.Date &&
                ak.Date <= l.EndDate.Date))
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
                EmpID = a.Employee.EmpID,
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
                // Additional navigation properties
                FullName = a.Employee.FullName,
                Code = a.Employee.Code,
                ShiftName = a.Shift?.Name,
                ApproverName = null // Placeholder – could be joined with Users table
            };
        }).ToList();

        return PaginatedResponse<GetNotAttendanceResponse>.Create(attendances, totalCount, query.Page, query.PageSize);
    }

    private static string GetDisplayName(LeaveType leaveType)
    {
        FieldInfo? field = leaveType.GetType().GetField(leaveType.ToString());
        DisplayAttribute? displayAttribute = field?.GetCustomAttribute<DisplayAttribute>();
        return displayAttribute?.Name ?? leaveType.ToString();
    }
}

