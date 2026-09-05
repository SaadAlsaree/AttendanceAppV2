using System.Reflection;
using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Models;
using Domain.Entities.Organizations;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Reports.GetOrganizationReport;

internal class GetOrganizationReportHandler(
    IApplicationDbContext context,
    IHasPermission hasPermission,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext
    )
    : IQueryHandler<GetOrganizationReportQuery, ApiResponse<GetOrganizationReportVm>>
{
    public async Task<Result<ApiResponse<GetOrganizationReportVm>>> Handle(GetOrganizationReportQuery query, CancellationToken cancellationToken)
    {
        try
        {
            // Get user info
            UserInfoDto user = await userContext.GetUserAsync();

            // A SiteSupervisor reports on their site's explicit unit list. They normally have no
            // OrganizationalUnitId at all, so the unit-based path below would fail with NotFound.
            // Note there is deliberately no sub-unit descent here: site membership is
            // non-transitive, and BuildOrganizationReportAsync already takes a flat unit list.
            if (user.Role == Role.SiteSupervisor)
            {
                return await HandleSiteSupervisorAsync(query, cancellationToken);
            }

            // Validate organizational unit exists
            OrganizationalUnit? organizationalUnit = await context.OrganizationalUnits
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == user.OrganizationalUnitId, cancellationToken);

            if (organizationalUnit is null)
            {
                return Result.Failure<ApiResponse<GetOrganizationReportVm>>(
                    Error.NotFound("OrganizationalUnit.NotFound", "الوحدة التنظيمية غير موجودة"));
            }

            // Set date with proper UTC conversion
            DateOnly reportDate = query.Date ?? DateOnly.FromDateTime(dateTimeProvider.GetCurrentLocalTime());
            DateTime reportDateTime = dateTimeProvider.EnsureUtc(reportDate.ToDateTime(TimeOnly.MinValue));

            // Get target unit IDs (main unit + sub units if requested)
            List<Guid> targetUnitIds = new() { user.OrganizationalUnitId ?? Guid.Empty };
            if (query.IncludeSubUnits)
            {
                List<Guid> subUnitIds = await GetSubUnitIdsRecursiveAsync(user.OrganizationalUnitId ?? Guid.Empty, cancellationToken);
                targetUnitIds.AddRange(subUnitIds);
            }

            // Build the report
            GetOrganizationReportVm report = await BuildOrganizationReportAsync(
                targetUnitIds,
                reportDate,
                query.ShiftId,
                query.SearchTerm,
                query.PageNumber,
                query.PageSize,
                cancellationToken);

            report.Date = reportDateTime;
            report.GeneratedAt = dateTimeProvider.GetCurrentLocalTime();

            return Result.Success(new ApiResponse<GetOrganizationReportVm>
            {
                Data = report,
                Message = "تم إنشاء تقرير الجهة بنجاح",
                IsSuccess = true
            });
        }
        catch (Exception ex)
        {
            return Result.Failure<ApiResponse<GetOrganizationReportVm>>(
                Error.Failure("Report.GenerationFailed", $"فشل في إنشاء التقرير: {ex.Message}"));
        }
    }

    /// <summary>
    /// Builds the report over a site's explicit unit list. Deliberately never calls
    /// <see cref="GetSubUnitIdsRecursiveAsync"/> — a site's membership does not extend to the
    /// children of its member units, so descending the tree here would report on units the
    /// supervisor has no access to.
    /// </summary>
    private async Task<Result<ApiResponse<GetOrganizationReportVm>>> HandleSiteSupervisorAsync(
        GetOrganizationReportQuery query,
        CancellationToken cancellationToken)
    {
        var siteUnitIds = (await hasPermission.GetAccessibleUnitIdsAsync(cancellationToken)).ToList();

        if (siteUnitIds.Count == 0)
        {
            return Result.Failure<ApiResponse<GetOrganizationReportVm>>(SiteErrors.NoSiteAssigned);
        }

        // If the caller narrowed to one unit it must be a member of their site; otherwise report
        // across the whole site.
        List<Guid> targetUnitIds;
        if (query.OrganizationalUnitId != Guid.Empty)
        {
            if (!siteUnitIds.Contains(query.OrganizationalUnitId))
            {
                return Result.Failure<ApiResponse<GetOrganizationReportVm>>(SiteErrors.AccessDenied);
            }

            targetUnitIds = new List<Guid> { query.OrganizationalUnitId };
        }
        else
        {
            targetUnitIds = siteUnitIds;
        }

        DateOnly reportDate = query.Date ?? DateOnly.FromDateTime(dateTimeProvider.GetCurrentLocalTime());

        GetOrganizationReportVm report = await BuildOrganizationReportAsync(
            targetUnitIds,
            reportDate,
            query.ShiftId,
            query.SearchTerm,
            query.PageNumber,
            query.PageSize,
            cancellationToken);

        report.Date = dateTimeProvider.EnsureUtc(reportDate.ToDateTime(TimeOnly.MinValue));
        report.GeneratedAt = dateTimeProvider.GetCurrentLocalTime();

        return Result.Success(new ApiResponse<GetOrganizationReportVm>
        {
            Data = report,
            Message = "تم إنشاء تقرير الموقع بنجاح",
            IsSuccess = true
        });
    }

    private async Task<List<Guid>> GetSubUnitIdsRecursiveAsync(Guid parentUnitId, CancellationToken cancellationToken)
    {
        List<Guid> allSubUnitIds = new();

        // Get direct children
        List<Guid> directChildren = await context.OrganizationalUnits
            .AsNoTracking()
            .Where(u => u.ParentUnitId == parentUnitId)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        allSubUnitIds.AddRange(directChildren);

        // Get children of children recursively
        foreach (Guid childId in directChildren)
        {
            List<Guid> grandChildren = await GetSubUnitIdsRecursiveAsync(childId, cancellationToken);
            allSubUnitIds.AddRange(grandChildren);
        }

        return allSubUnitIds;
    }

    private async Task<GetOrganizationReportVm> BuildOrganizationReportAsync(
        List<Guid> unitIds,
        DateOnly date,
        Guid? shiftId,
        string? searchTerm,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {

        var report = new GetOrganizationReportVm();

        // The report headcount is the actual unit population and must not change
        // when the employee-detail list is searched or paginated.
        IQueryable<Employee> employeesQuery = context.Employees
            .AsNoTracking()
            .Where(e => unitIds.Contains(e.OrganizationalUnitId ?? Guid.Empty));

        // Get total employee count
        report.TotalEmployees = await employeesQuery.CountAsync(cancellationToken);

        // Get attendance data for the specific date
        IQueryable<Domain.Entities.Attendance.Attendance> attendanceQuery = context.Attendances
            .AsNoTracking()
            .Include(a => a.Employee)
            .Include(a => a.Employee.OrganizationalUnit)
            .Where(a => unitIds.Contains(a.Employee.OrganizationalUnitId ?? Guid.Empty) &&
                       DateOnly.FromDateTime(a.Date) == date &&
                       (a.CheckInTime != null || a.CheckOutTime != null));

        if (shiftId.HasValue)
        {
            attendanceQuery = attendanceQuery.Where(a => a.ShiftId == shiftId);
        }

        List<Domain.Entities.Attendance.Attendance> attendances = await attendanceQuery
            .OrderBy(a => a.CheckInTime ?? a.Date)
            .ToListAsync(cancellationToken);

        // Get leave data for the specific date (needed before computing non-fingerprinted)
        int totalLeaves = await context.Leaves
               .Where(l => DateOnly.FromDateTime(l.StartDate) <= date &&
                          DateOnly.FromDateTime(l.EndDate) >= date &&
                          l.Status == LeaveStatus.Approved &&
                          unitIds.Contains(l.Employee.OrganizationalUnitId ?? Guid.Empty))
               .CountAsync(cancellationToken);

        report.TotalLeaves = totalLeaves;

        // Calculate overall statistics
        report.TotalAttendances = attendances.Count;
        // Non-fingerprinted (غير مبصمين): scheduled for the day but did not clock in, excluding those on leave
        report.TotalNotAttendances = (await GetNonFingerprintedAsync(unitIds, date, cancellationToken)).Count;
        report.TotalLate = attendances.Count(a => a.LateMinutes > 0);
        report.TotalOvertime = attendances.Count(a => a.OvertimeMinutes > 0);

        // Build unit summaries
        await BuildUnitSummariesAsync(report, unitIds, attendances, date, searchTerm, pageNumber, pageSize, cancellationToken);

        return report;
    }

    // Non-fingerprinted (غير مبصمين): employees scheduled for the day (have a ShiftId) who did not clock in
    // (no check-in and no check-out), excluding those on approved leave that day. Mirrors GetOrganizationalSummaryHandler.
    // Returns the actual employee names so the printed report can list them (count = result.Count).
    private async Task<List<NonFingerprintedEmployee>> GetNonFingerprintedAsync(List<Guid> unitIds, DateOnly date, CancellationToken cancellationToken)
    {
        List<Guid> employeesOnLeave = await context.Leaves
            .Where(l => DateOnly.FromDateTime(l.StartDate) <= date &&
                       DateOnly.FromDateTime(l.EndDate) >= date &&
                       l.Status == LeaveStatus.Approved &&
                       unitIds.Contains(l.Employee.OrganizationalUnitId ?? Guid.Empty))
            .Select(l => l.EmployeeId)
            .ToListAsync(cancellationToken);

        return await context.Attendances
            .AsNoTracking()
            .Where(a => DateOnly.FromDateTime(a.Date) == date &&
                       unitIds.Contains(a.Employee.OrganizationalUnitId ?? Guid.Empty) &&
                       a.ShiftId != null &&
                       a.CheckInTime == null &&
                       a.CheckOutTime == null &&
                       !employeesOnLeave.Contains(a.EmployeeId))
            .Select(a => new NonFingerprintedEmployee
            {
                EmployeeId = a.EmployeeId,
                EmployeeName = a.Employee.FullName
            })
            .ToListAsync(cancellationToken);
    }

    // Deliberate status actions (الاجراءات) recorded on an attendance row. Excludes the "present" punch
    // statuses (Present/Late/Early_Out/Overtime — those belong in the present listing) and Pending
    // (the unprocessed bucket that the non-fingerprinted list already covers).
    private static readonly AttendanceStatus[] ActionStatuses =
    [
        AttendanceStatus.Absent,
        AttendanceStatus.Break,
        AttendanceStatus.Vacation,
        AttendanceStatus.Holiday,
        AttendanceStatus.Duty,
        AttendanceStatus.Exempted,
        AttendanceStatus.Permitted
    ];

    // Actions/statuses (الاجراءات): employees who have a status action for the day — approved leaves
    // (labeled by leave type) plus attendance records carrying a deliberate status (see ActionStatuses).
    // One row per employee.
    private async Task<List<ActionEmployee>> GetActionEmployeesAsync(Guid unitId, DateOnly date, CancellationToken cancellationToken)
    {
        var leaveActions = await context.Leaves
            .AsNoTracking()
            .Where(l => DateOnly.FromDateTime(l.StartDate) <= date &&
                       DateOnly.FromDateTime(l.EndDate) >= date &&
                       l.Status == LeaveStatus.Approved &&
                       l.Employee.OrganizationalUnitId == unitId)
            .Select(l => new { l.EmployeeId, l.Employee.FullName, l.LeaveType })
            .ToListAsync(cancellationToken);

        var attendanceActions = await context.Attendances
            .AsNoTracking()
            .Where(a => DateOnly.FromDateTime(a.Date) == date &&
                       a.Employee.OrganizationalUnitId == unitId &&
                       ActionStatuses.Contains(a.Status))
            .Select(a => new { a.EmployeeId, a.Employee.FullName, a.Status })
            .ToListAsync(cancellationToken);

        var actions = new List<ActionEmployee>();
        var seen = new HashSet<Guid>();

        foreach (var leave in leaveActions)
        {
            if (seen.Add(leave.EmployeeId))
            {
                actions.Add(new ActionEmployee
                {
                    EmployeeId = leave.EmployeeId,
                    EmployeeName = leave.FullName,
                    ActionName = GetEnumDisplayName(leave.LeaveType)
                });
            }
        }

        foreach (var attendance in attendanceActions)
        {
            if (seen.Add(attendance.EmployeeId))
            {
                actions.Add(new ActionEmployee
                {
                    EmployeeId = attendance.EmployeeId,
                    EmployeeName = attendance.FullName,
                    ActionName = GetEnumDisplayName(attendance.Status)
                });
            }
        }

        return actions;
    }

    // Resolve an enum value's [Display(Name = "...")] Arabic label, falling back to the enum name.
    private static string GetEnumDisplayName<TEnum>(TEnum value) where TEnum : struct, Enum
    {
        System.ComponentModel.DataAnnotations.DisplayAttribute? displayAttribute = typeof(TEnum)
            .GetField(value.ToString())
            ?.GetCustomAttribute<System.ComponentModel.DataAnnotations.DisplayAttribute>();

        return displayAttribute?.Name ?? value.ToString();
    }

    private async Task BuildUnitSummariesAsync(
        GetOrganizationReportVm report,
        List<Guid> unitIds,
        List<Domain.Entities.Attendance.Attendance> attendances,
        DateOnly date,
        string? searchTerm,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        // Get organizational units with their details
        List<OrganizationalUnit> units = await context.OrganizationalUnits
            .AsNoTracking()
            .Include(u => u.ParentUnit)
            .Where(u => unitIds.Contains(u.Id))
            .ToListAsync(cancellationToken);

        foreach (OrganizationalUnit unit in units)
        {
            var unitSummary = new UnitSummary
            {
                UnitId = unit.Id,
                UnitName = unit.UnitName,
                UnitCode = unit.UnitCode,
                ParentUnitId = unit.ParentUnitId,
                ParentUnitName = unit.ParentUnit?.UnitName
            };

            // Keep the real unit headcount independent from search/pagination.
            IQueryable<Employee> unitEmployeesQuery = context.Employees
                .AsNoTracking()
                .Where(e => e.OrganizationalUnitId == unit.Id);

            unitSummary.TotalEmployees = await unitEmployeesQuery.CountAsync(cancellationToken);

            IQueryable<Employee> filteredUnitEmployeesQuery = unitEmployeesQuery;

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                filteredUnitEmployeesQuery = filteredUnitEmployeesQuery.Where(e =>
                    e.FullName.Contains(searchTerm) ||
                    //e.Code.Contains(searchTerm) ||
                    e.Email!.Contains(searchTerm));
            }

            // Get shifts count for this unit
            unitSummary.TotalShifts = await context.Shifts
                .AsNoTracking()
                .CountAsync(cancellationToken);

            // Filter attendances for this unit
            var unitAttendances = attendances
                .Where(a => a.Employee.OrganizationalUnitId == unit.Id)
                .ToList();

            // Per-unit leaves for the specific date
            unitSummary.TotalLeaves = await context.Leaves
                .Where(l => DateOnly.FromDateTime(l.StartDate) <= date &&
                           DateOnly.FromDateTime(l.EndDate) >= date &&
                           l.Status == LeaveStatus.Approved &&
                           l.Employee.OrganizationalUnitId == unit.Id)
                .CountAsync(cancellationToken);

            // Calculate unit statistics
            unitSummary.TotalAttendances = unitAttendances.Count;
            // Non-fingerprinted (غير مبصمين): scheduled for the day but did not clock in, excluding those on leave.
            // Keep names (for print) and count consistent from the same query.
            unitSummary.NonFingerprintedEmployees = await GetNonFingerprintedAsync([unit.Id], date, cancellationToken);
            unitSummary.TotalNotAttendances = unitSummary.NonFingerprintedEmployees.Count;
            // Actions/statuses (الاجراءات) for the day — leaves + non-present attendance statuses.
            unitSummary.ActionEmployees = await GetActionEmployeesAsync(unit.Id, date, cancellationToken);
            unitSummary.TotalLate = unitAttendances.Count(a => a.LateMinutes > 0);
            unitSummary.TotalOvertime = unitAttendances.Count(a => a.OvertimeMinutes > 0);

            // Get employee details with pagination
            List<Employee> unitEmployees = await filteredUnitEmployeesQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            // Build employee attendance details
            foreach (Employee employee in unitEmployees)
            {
                var employeeAttendances = unitAttendances
                    .Where(a => a.EmployeeId == employee.Id)
                    .OrderBy(a => a.CheckInTime ?? a.Date)
                    .ToList();

                foreach (Domain.Entities.Attendance.Attendance attendance in employeeAttendances)
                {
                    var employeeDetail = new EmployeeAttendanceDetail
                    {
                        EmployeeId = employee.Id,
                        EmployeeName = employee.FullName,
                        //EmployeeCode = employee.Code,
                        OrganizationalUnitId = unit.Id,
                        OrganizationalUnitName = unit.UnitName,
                        Date = attendance.Date,
                        CheckInTime = attendance.CheckInTime,
                        CheckOutTime = attendance.CheckOutTime,
                        IsLate = attendance.LateMinutes > 0,
                        IsEarlyLeave = attendance.EarlyLeaveMinutes > 0,
                        IsOnLeave = attendance.Status == AttendanceStatus.Vacation,
                        IsAbsent = attendance.Status == AttendanceStatus.Absent,
                        OvertimeDuration = attendance.OvertimeMinutes.HasValue ? TimeSpan.FromMinutes(attendance.OvertimeMinutes.Value) : null
                    };

                    unitSummary.EmployeeDetails.Add(employeeDetail);
                }
            }

            report.Units.Add(unitSummary);
        }
    }

}
