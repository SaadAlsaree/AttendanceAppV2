using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Models;
using Domain.Enums;
using Domain.Reports;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Attendance.Reports.GetOvertimeReport;

internal sealed class GetOvertimeReportQueryHandler(
    IApplicationDbContext context,
    IHasPermission hasPermission,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetOvertimeReportQuery, OvertimeReportResponse>
{
    public async Task<Result<OvertimeReportResponse>> Handle(GetOvertimeReportQuery query, CancellationToken cancellationToken)
    {
        // Validate date range
        if (query.StartDate >= query.EndDate)
        {
            return Result.Failure<OvertimeReportResponse>(ReportErrors.InvalidDateRange(query.StartDate, query.EndDate));
        }

        // Build base query for attendance data with overtime
        IQueryable<Domain.Entities.Attendance.Attendance> attendanceQuery = context.Attendances
            .AsNoTracking()
            .Include(a => a.Employee)
            .ThenInclude(e => e.OrganizationalUnit)
            .Where(a => a.Date >= query.StartDate && a.Date <= query.EndDate)
            .Where(a => a.OvertimeMinutes > 0);

        // check if user role not Admin then apply accessible unit ids filter

        UserInfoDto user = await userContext.GetUserAsync();
        if (user.Role != Role.Admin)
        {
            IEnumerable<Guid> accessibleUnitIds = await hasPermission.GetAccessibleUnitIdsAsync(cancellationToken);
            attendanceQuery = attendanceQuery.Where(a => accessibleUnitIds.Contains(a.Employee.OrganizationalUnitId!.Value));
        }

        // Apply filters
        if (query.OrganizationId.HasValue)
        {
            attendanceQuery = attendanceQuery.Where(a => a.OrganizationId == query.OrganizationId.Value);
        }

        if (query.OrganizationalUnitId.HasValue)
        {
            attendanceQuery = attendanceQuery.Where(a => a.Employee.OrganizationalUnitId == query.OrganizationalUnitId.Value);
        }

        if (query.EmployeeId.HasValue)
        {
            attendanceQuery = attendanceQuery.Where(a => a.EmployeeId == query.EmployeeId.Value);
        }

        if (!string.IsNullOrEmpty(query.OvertimeType))
        {
            // TODO: Implement overtime type filtering based on business logic
            // This would depend on how overtime types are stored/calculated
        }

        if (query.MinOvertimeHours.HasValue)
        {
            int minMinutes = query.MinOvertimeHours.Value * 60;
            attendanceQuery = attendanceQuery.Where(a => a.OvertimeMinutes >= minMinutes);
        }

        List<Domain.Entities.Attendance.Attendance> attendances = await attendanceQuery.ToListAsync(cancellationToken);

        if (!attendances.Any())
        {
            // Return empty response instead of error when no data is found
            var emptyResponse = new OvertimeReportResponse
            {
                StartDate = query.StartDate,
                EndDate = query.EndDate,
                ReportType = query.ReportType,
                Statistics = new OvertimeStatistics
                {
                    TotalEmployees = 0,
                    EmployeesWithOvertime = 0,
                    TotalOvertimeHours = 0,
                    AverageOvertimeHours = 0,
                    TotalOvertimeCost = 0,
                    AverageOvertimeCost = 0,
                    RegularOvertimeCount = 0,
                    HolidayOvertimeCount = 0,
                    WeekendOvertimeCount = 0
                },
                EmployeeSummaries = new List<EmployeeOvertimeSummary>(),
                DepartmentSummaries = new List<DepartmentOvertimeSummary>(),
                GeneratedAt = dateTimeProvider.Now
            };

            return emptyResponse;
        }

        // Calculate statistics
        OvertimeStatistics statistics = CalculateOvertimeStatistics(attendances);

        // Generate employee summaries
        List<EmployeeOvertimeSummary> employeeSummaries = GenerateEmployeeOvertimeSummaries(attendances);

        // Generate department summaries
        List<DepartmentOvertimeSummary> departmentSummaries = GenerateDepartmentOvertimeSummaries(attendances);

        var response = new OvertimeReportResponse
        {
            StartDate = query.StartDate,
            EndDate = query.EndDate,
            ReportType = query.ReportType,
            Statistics = statistics,
            EmployeeSummaries = employeeSummaries,
            DepartmentSummaries = departmentSummaries,
            GeneratedAt = dateTimeProvider.Now
        };

        return response;
    }

    private static OvertimeStatistics CalculateOvertimeStatistics(List<Domain.Entities.Attendance.Attendance> attendances)
    {
        IEnumerable<Guid> employeeIds = attendances.Select(a => a.EmployeeId).Distinct();
        int totalEmployees = employeeIds.Count();
        int employeesWithOvertime = totalEmployees;
        decimal totalOvertimeHours = attendances.Sum(a => a.OvertimeMinutes ?? 0) / 60.0m;
        decimal averageOvertimeHours = totalEmployees > 0 ? totalOvertimeHours / totalEmployees : 0;

        // TODO: Calculate overtime costs based on employee hourly rates
        decimal totalOvertimeCost = 0; // Placeholder for cost calculation
        decimal averageOvertimeCost = 0; // Placeholder for cost calculation

        // TODO: Calculate overtime type counts based on business logic
        int regularOvertimeCount = attendances.Count; // Placeholder
        int holidayOvertimeCount = 0; // Placeholder
        int weekendOvertimeCount = 0; // Placeholder

        return new OvertimeStatistics
        {
            TotalEmployees = totalEmployees,
            EmployeesWithOvertime = employeesWithOvertime,
            TotalOvertimeHours = totalOvertimeHours,
            AverageOvertimeHours = averageOvertimeHours,
            TotalOvertimeCost = totalOvertimeCost,
            AverageOvertimeCost = averageOvertimeCost,
            RegularOvertimeCount = regularOvertimeCount,
            HolidayOvertimeCount = holidayOvertimeCount,
            WeekendOvertimeCount = weekendOvertimeCount
        };
    }

    private static List<EmployeeOvertimeSummary> GenerateEmployeeOvertimeSummaries(
        List<Domain.Entities.Attendance.Attendance> attendances)
    {
        var summaries = attendances
            .GroupBy(a => a.EmployeeId)
            .Select(g => new EmployeeOvertimeSummary
            {
                EmployeeId = g.Key,
                EmployeeName = g.First().Employee.FullName,
                DepartmentName = g.First().Employee.OrganizationalUnit?.UnitName ?? "Unknown",
                TotalOvertimeHours = g.Sum(a => a.OvertimeMinutes ?? 0) / 60.0m,
                RegularOvertimeHours = g.Sum(a => a.OvertimeMinutes ?? 0) / 60.0m, // Placeholder
                HolidayOvertimeHours = 0, // Placeholder
                WeekendOvertimeHours = 0, // Placeholder
                OvertimeCost = 0, // Placeholder
                OvertimeDays = g.Count(),
                AverageOvertimePerDay = g.Any() ? g.Sum(a => a.OvertimeMinutes ?? 0) / 60.0m / g.Count() : 0
            })
            .ToList();

        return summaries;
    }

    private static List<DepartmentOvertimeSummary> GenerateDepartmentOvertimeSummaries(
        List<Domain.Entities.Attendance.Attendance> attendances)
    {
        var summaries = attendances
            .GroupBy(a => a.Employee.OrganizationalUnitId)
            .Select(g => new DepartmentOvertimeSummary
            {
                DepartmentId = g.Key ?? Guid.Empty,
                DepartmentName = g.First().Employee.OrganizationalUnit?.UnitName ?? "Unknown",
                TotalEmployees = g.Select(a => a.EmployeeId).Distinct().Count(),
                EmployeesWithOvertime = g.Select(a => a.EmployeeId).Distinct().Count(),
                TotalOvertimeHours = g.Sum(a => a.OvertimeMinutes ?? 0) / 60.0m,
                AverageOvertimeHours = g.Select(a => a.EmployeeId).Distinct().Any()
                    ? g.Sum(a => a.OvertimeMinutes ?? 0) / 60.0m / g.Select(a => a.EmployeeId).Distinct().Count()
                    : 0,
                TotalOvertimeCost = 0, // Placeholder
                AverageOvertimeCost = 0 // Placeholder
            })
            .ToList();

        return summaries;
    }
}
