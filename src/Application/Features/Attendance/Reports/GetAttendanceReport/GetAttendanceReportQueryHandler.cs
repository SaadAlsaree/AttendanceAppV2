using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Models;
using Domain.Enums;
using Domain.Reports;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Attendance.Reports.GetAttendanceReport;

internal sealed class GetAttendanceReportQueryHandler(
    IApplicationDbContext context,
    IHasPermission hasPermission,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetAttendanceReportQuery, AttendanceReportResponse>
{
    public async Task<Result<AttendanceReportResponse>> Handle(GetAttendanceReportQuery query, CancellationToken cancellationToken)
    {
        // Validate date range
        if (query.StartDate >= query.EndDate)
        {
            return Result.Failure<AttendanceReportResponse>(ReportErrors.InvalidDateRange(query.StartDate, query.EndDate));
        }

        // Build base query for attendance data
        IQueryable<Domain.Entities.Attendance.Attendance> attendanceQuery = context.Attendances
            .AsNoTracking()
            .Include(a => a.Employee)
            .ThenInclude(e => e.OrganizationalUnit)
            .Where(a => a.Date >= query.StartDate && a.Date <= query.EndDate);

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

        if (query.ManagerId.HasValue)
        {
            attendanceQuery = attendanceQuery.Where(a => a.Employee.ManagerId == query.ManagerId.Value);
        }

        List<Domain.Entities.Attendance.Attendance> attendances = await attendanceQuery.ToListAsync(cancellationToken);

        if (!attendances.Any())
        {
            // Return empty response instead of error when no data is found
            var emptyResponse = new AttendanceReportResponse
            {
                StartDate = query.StartDate,
                EndDate = query.EndDate,
                ReportType = query.ReportType,
                Statistics = new AttendanceStatistics
                {
                    TotalEmployees = 0,
                    PresentEmployees = 0,
                    AbsentEmployees = 0,
                    LateArrivals = 0,
                    EarlyDepartures = 0,
                    TotalOvertimeHours = 0,
                    AverageAttendanceRate = 0
                },
                EmployeeSummaries = new List<EmployeeAttendanceSummary>(),
                DepartmentSummaries = new List<DepartmentAttendanceSummary>(),
                GeneratedAt = dateTimeProvider.Now
            };

            return emptyResponse;
        }

        // Calculate statistics
        AttendanceStatistics statistics = CalculateStatistics(attendances);

        // Generate employee summaries
        List<EmployeeAttendanceSummary> employeeSummaries = GenerateEmployeeSummaries(attendances, query.IncludeOvertime);

        // Generate department summaries
        List<DepartmentAttendanceSummary> departmentSummaries = GenerateDepartmentSummaries(attendances, query.IncludeOvertime);

        var response = new AttendanceReportResponse
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

    private static AttendanceStatistics CalculateStatistics(List<Domain.Entities.Attendance.Attendance> attendances)
    {
        int totalEmployees = attendances.Select(a => a.EmployeeId).Distinct().Count();
        int presentEmployees = attendances.Count(a => a.Status == AttendanceStatus.Present);
        int absentEmployees = attendances.Count(a => a.Status == AttendanceStatus.Absent);
        int lateArrivals = attendances.Count(a => a.LateMinutes > 0);
        int earlyDepartures = attendances.Count(a => a.EarlyLeaveMinutes > 0);
        decimal totalOvertimeHours = attendances.Sum(a => a.OvertimeMinutes ?? 0) / 60.0m;
        decimal averageAttendanceRate = totalEmployees > 0 ? (decimal)presentEmployees / totalEmployees * 100 : 0;

        return new AttendanceStatistics
        {
            TotalEmployees = totalEmployees,
            PresentEmployees = presentEmployees,
            AbsentEmployees = absentEmployees,
            LateArrivals = lateArrivals,
            EarlyDepartures = earlyDepartures,
            TotalOvertimeHours = totalOvertimeHours,
            AverageAttendanceRate = averageAttendanceRate
        };
    }

    private static List<EmployeeAttendanceSummary> GenerateEmployeeSummaries(
        List<Domain.Entities.Attendance.Attendance> attendances,
        bool includeOvertime)
    {
        var summaries = attendances
            .GroupBy(a => a.EmployeeId)
            .Select(g => new EmployeeAttendanceSummary
            {
                EmployeeId = g.Key,
                FullName = g.First().Employee.FullName,
                DepartmentName = g.First().Employee.OrganizationalUnit?.UnitName ?? "Unknown",
                PresentDays = g.Count(a => a.Status == AttendanceStatus.Present),
                AbsentDays = g.Count(a => a.Status == AttendanceStatus.Absent),
                LateArrivals = g.Count(a => a.LateMinutes > 0),
                EarlyDepartures = g.Count(a => a.EarlyLeaveMinutes > 0),
                TotalOvertimeHours = includeOvertime ? g.Sum(a => a.OvertimeMinutes ?? 0) / 60.0m : 0,
                AttendanceRate = g.Any() ? (decimal)g.Count(a => a.Status == AttendanceStatus.Present) / g.Count() * 100 : 0
            })
            .ToList();

        return summaries;
    }

    private static List<DepartmentAttendanceSummary> GenerateDepartmentSummaries(
        List<Domain.Entities.Attendance.Attendance> attendances,
        bool includeOvertime)
    {
        var summaries = attendances
            .GroupBy(a => a.Employee.OrganizationalUnitId)
            .Select(g => new DepartmentAttendanceSummary
            {
                DepartmentId = g.Key ?? Guid.Empty,
                DepartmentName = g.First().Employee.OrganizationalUnit?.UnitName ?? "Unknown",
                TotalEmployees = g.Select(a => a.EmployeeId).Distinct().Count(),
                PresentEmployees = g.Where(a => a.Status == AttendanceStatus.Present).Select(a => a.EmployeeId).Distinct().Count(),
                AbsentEmployees = g.Where(a => a.Status == AttendanceStatus.Absent).Select(a => a.EmployeeId).Distinct().Count(),
                TotalOvertimeHours = includeOvertime ? g.Sum(a => a.OvertimeMinutes ?? 0) / 60.0m : 0,
                AverageAttendanceRate = g.Select(a => a.EmployeeId).Distinct().Any()
                    ? (decimal)g.Where(a => a.Status == AttendanceStatus.Present).Select(a => a.EmployeeId).Distinct().Count()
                        / g.Select(a => a.EmployeeId).Distinct().Count() * 100
                    : 0
            })
            .ToList();

        return summaries;
    }
}
