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
    //IHasPermission hasPermission,
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

        // Get user info
        UserInfoDto user = await userContext.GetUserAsync();
        var report = new GetOrganizationReportVm();

        // Get all employees in target units
        IQueryable<Employee> employeesQuery = context.Employees
            .AsNoTracking()
            .Where(e => unitIds.Contains(user.OrganizationalUnitId ?? Guid.Empty));

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            employeesQuery = employeesQuery.Where(e =>
                e.FullName.Contains(searchTerm) ||
                //e.Code.Contains(searchTerm) ||
                e.Email!.Contains(searchTerm));
        }

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

        // Calculate overall statistics
        report.TotalAttendances = attendances.Count;
        report.TotalNotAttendances = attendances.Count - report.TotalAttendances - report.TotalLeaves;
        report.TotalLate = attendances.Count(a => a.LateMinutes > 0);
        report.TotalOvertime = attendances.Count(a => a.OvertimeMinutes > 0);

        // Get leave data for the specific date
        int totalLeaves = await context.Leaves
               .Where(l => DateOnly.FromDateTime(l.StartDate) <= date &&
                          DateOnly.FromDateTime(l.EndDate) >= date &&
                          unitIds.Contains(l.Employee.OrganizationalUnitId ?? Guid.Empty))
               .CountAsync(cancellationToken);

        report.TotalLeaves = totalLeaves;

        // Build unit summaries
        await BuildUnitSummariesAsync(report, unitIds, attendances, totalLeaves, searchTerm, pageNumber, pageSize, cancellationToken);

        return report;
    }

    private async Task BuildUnitSummariesAsync(
        GetOrganizationReportVm report,
        List<Guid> unitIds,
        List<Domain.Entities.Attendance.Attendance> attendances,
        int totalLeaves,
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

            // Get employees for this unit
            IQueryable<Employee> unitEmployeesQuery = context.Employees
                .AsNoTracking()
                .Where(e => e.OrganizationalUnitId == unit.Id);

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                unitEmployeesQuery = unitEmployeesQuery.Where(e =>
                    e.FullName.Contains(searchTerm) ||
                    //e.Code.Contains(searchTerm) ||
                    e.Email!.Contains(searchTerm));
            }

            unitSummary.TotalEmployees = await unitEmployeesQuery.CountAsync(cancellationToken);

            // Get shifts count for this unit
            unitSummary.TotalShifts = await context.Shifts
                .AsNoTracking()
                .CountAsync(cancellationToken);

            // Filter attendances for this unit
            var unitAttendances = attendances
                .Where(a => a.Employee.OrganizationalUnitId == unit.Id)
                .ToList();

            // Calculate unit statistics
            unitSummary.TotalAttendances = unitAttendances.Count;
            unitSummary.TotalNotAttendances = unitSummary.TotalEmployees - unitSummary.TotalAttendances - unitSummary.TotalLeaves;
            unitSummary.TotalLate = unitAttendances.Count(a => a.LateMinutes > 0);
            unitSummary.TotalOvertime = unitAttendances.Count(a => a.OvertimeMinutes > 0);

            // Filter leaves for this unit
            unitSummary.TotalLeaves = totalLeaves;

            // Get employee details with pagination
            List<Employee> unitEmployees = await unitEmployeesQuery
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
