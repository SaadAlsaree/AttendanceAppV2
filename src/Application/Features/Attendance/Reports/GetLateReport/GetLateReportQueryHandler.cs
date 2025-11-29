using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Models;
using Domain.Enums;
using Domain.Reports;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Attendance.Reports.GetLateReport;

internal sealed class GetLateReportQueryHandler(
    IApplicationDbContext context,
    IHasPermission hasPermission,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetLateReportQuery, LateReportResponse>
{
    public async Task<Result<LateReportResponse>> Handle(GetLateReportQuery query, CancellationToken cancellationToken)
    {
        // Validate date range
        if (query.StartDate >= query.EndDate)
        {
            return Result.Failure<LateReportResponse>(ReportErrors.InvalidDateRange(query.StartDate, query.EndDate));
        }

        // Build base query for attendance data with late arrivals
        IQueryable<Domain.Entities.Attendance.Attendance> attendanceQuery = context.Attendances
            .AsNoTracking()
            .Include(a => a.Employee)
            .ThenInclude(e => e.OrganizationalUnit)
            .Where(a => a.Date >= query.StartDate && a.Date <= query.EndDate)
            .Where(a => a.LateMinutes > 0);

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

        if (query.MinLateMinutes.HasValue)
        {
            attendanceQuery = attendanceQuery.Where(a => a.LateMinutes.HasValue && a.LateMinutes.Value >= query.MinLateMinutes.Value);
        }

        List<Domain.Entities.Attendance.Attendance> attendances = await attendanceQuery.ToListAsync(cancellationToken);

        if (!attendances.Any())
        {
            // Return empty response instead of error when no data is found
            var emptyResponse = new LateReportResponse
            {
                StartDate = query.StartDate,
                EndDate = query.EndDate,
                ReportType = query.ReportType,
                Statistics = new LateStatistics
                {
                    TotalEmployees = 0,
                    EmployeesWithLateArrivals = 0,
                    TotalLateArrivals = 0,
                    AverageLateMinutes = 0,
                    TotalLateMinutes = 0,
                    FrequentLateArrivals = 0,
                    LateArrivalRate = 0
                },
                EmployeeSummaries = new List<EmployeeLateSummary>(),
                DepartmentSummaries = new List<DepartmentLateSummary>(),
                GeneratedAt = dateTimeProvider.Now
            };

            return emptyResponse;
        }

        // Calculate statistics
        LateStatistics statistics = CalculateLateStatistics(attendances);

        // Generate employee summaries
        List<EmployeeLateSummary> employeeSummaries = GenerateEmployeeLateSummaries(attendances);

        // Generate department summaries
        List<DepartmentLateSummary> departmentSummaries = GenerateDepartmentLateSummaries(attendances);

        var response = new LateReportResponse
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

    private static LateStatistics CalculateLateStatistics(List<Domain.Entities.Attendance.Attendance> attendances)
    {
        IEnumerable<Guid> employeeIds = attendances.Select(a => a.EmployeeId).Distinct();
        int totalEmployees = employeeIds.Count();
        int employeesWithLateArrivals = totalEmployees;
        int totalLateArrivals = attendances.Count;
        decimal totalLateMinutes = attendances.Sum(a => a.LateMinutes ?? 0);
        decimal averageLateMinutes = totalLateArrivals > 0 ? totalLateMinutes / totalLateArrivals : 0;

        // Calculate frequent late arrivals (more than 3 times in period)
        int frequentLateArrivals = attendances
            .GroupBy(a => a.EmployeeId)
            .Count(g => g.Count() > 3);

        // Calculate late arrival rate
        decimal lateArrivalRate = totalLateArrivals > 0 ? (decimal)totalLateArrivals / attendances.Count * 100 : 0;

        return new LateStatistics
        {
            TotalEmployees = totalEmployees,
            EmployeesWithLateArrivals = employeesWithLateArrivals,
            TotalLateArrivals = totalLateArrivals,
            AverageLateMinutes = averageLateMinutes,
            TotalLateMinutes = totalLateMinutes,
            FrequentLateArrivals = frequentLateArrivals,
            LateArrivalRate = lateArrivalRate
        };
    }

    private static List<EmployeeLateSummary> GenerateEmployeeLateSummaries(
        List<Domain.Entities.Attendance.Attendance> attendances)
    {
        var summaries = attendances
            .GroupBy(a => a.EmployeeId)
            .Select(g => new EmployeeLateSummary
            {
                EmployeeId = g.Key,
                FullName = g.First().Employee.FullName,
                DepartmentName = g.First().Employee.OrganizationalUnit?.UnitName ?? "Unknown",
                LateArrivalDays = g.Count(),
                TotalLateMinutes = g.Sum(a => a.LateMinutes ?? 0),
                AverageLateMinutes = g.Any() ? g.Sum(a => a.LateMinutes ?? 0) / g.Count() : 0,
                MostFrequentLateDay = CalculateMostFrequentLateDay(g.ToList()),
                LateArrivalRate = g.Any() ? (decimal)g.Count() / g.Count() * 100 : 0 // Placeholder calculation
            })
            .ToList();

        return summaries;
    }

    private static List<DepartmentLateSummary> GenerateDepartmentLateSummaries(
        List<Domain.Entities.Attendance.Attendance> attendances)
    {
        var summaries = attendances
            .GroupBy(a => a.Employee.OrganizationalUnitId)
            .Select(g => new DepartmentLateSummary
            {
                DepartmentId = g.Key ?? Guid.Empty,
                DepartmentName = g.First().Employee.OrganizationalUnit?.UnitName ?? "Unknown",
                TotalEmployees = g.Select(a => a.EmployeeId).Distinct().Count(),
                EmployeesWithLateArrivals = g.Select(a => a.EmployeeId).Distinct().Count(),
                TotalLateArrivals = g.Count(),
                AverageLateMinutes = g.Any() ? g.Sum(a => a.LateMinutes ?? 0) / g.Count() : 0,
                LateArrivalRate = g.Select(a => a.EmployeeId).Distinct().Any()
                    ? (decimal)g.Select(a => a.EmployeeId).Distinct().Count() / g.Select(a => a.EmployeeId).Distinct().Count() * 100
                    : 0
            })
            .ToList();

        return summaries;
    }

    private static int CalculateMostFrequentLateDay(List<Domain.Entities.Attendance.Attendance> attendances)
    {
        if (!attendances.Any())
        {
            return 1; // Default to Monday if no data
        }

        IGrouping<System.DayOfWeek, Domain.Entities.Attendance.Attendance>? dayOfWeekCounts = attendances
            .GroupBy(a => a.Date.DayOfWeek)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();

        return dayOfWeekCounts?.Key switch
        {
            System.DayOfWeek.Monday => 1,
            System.DayOfWeek.Tuesday => 2,
            System.DayOfWeek.Wednesday => 3,
            System.DayOfWeek.Thursday => 4,
            System.DayOfWeek.Friday => 5,
            System.DayOfWeek.Saturday => 6,
            System.DayOfWeek.Sunday => 7,
            _ => 1 // Default to Monday
        };
    }
}
