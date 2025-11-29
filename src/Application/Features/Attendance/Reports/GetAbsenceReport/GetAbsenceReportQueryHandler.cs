using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Models;
using Domain.Entities.Attendance;
using Domain.Entities.Organizations;
using Domain.Enums;
using Domain.Reports;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Attendance.Reports.GetAbsenceReport;

internal sealed class GetAbsenceReportQueryHandler(
    IApplicationDbContext context,
    IHasPermission hasPermission,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetAbsenceReportQuery, AbsenceReportResponse>
{
    public async Task<Result<AbsenceReportResponse>> Handle(GetAbsenceReportQuery query, CancellationToken cancellationToken)
    {
        // Validate date range
        if (query.StartDate >= query.EndDate)
        {
            return Result.Failure<AbsenceReportResponse>(ReportErrors.InvalidDateRange(query.StartDate, query.EndDate));
        }

        // Build base query for attendance data with absences
        IQueryable<Domain.Entities.Attendance.Attendance> attendanceQuery = context.Attendances
            .AsNoTracking()
            .Include(a => a.Employee)
            .ThenInclude(e => e.OrganizationalUnit)
            .Where(a => a.Date >= query.StartDate && a.Date <= query.EndDate)
            .Where(a => a.Status == AttendanceStatus.Absent);

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

        if (query.AbsenceType.HasValue)
        {
            attendanceQuery = attendanceQuery.Where(a => a.Status == query.AbsenceType.Value);
        }

        if (query.MinAbsenceDays.HasValue)
        {
            // Filter employees who have at least the minimum number of absence days
            List<Guid> employeeIdsWithMinAbsences = await attendanceQuery
                .GroupBy(a => a.EmployeeId)
                .Where(g => g.Count() >= query.MinAbsenceDays.Value)
                .Select(g => g.Key)
                .ToListAsync(cancellationToken);

            attendanceQuery = attendanceQuery.Where(a => employeeIdsWithMinAbsences.Contains(a.EmployeeId));
        }

        List<Domain.Entities.Attendance.Attendance> attendances = await attendanceQuery.ToListAsync(cancellationToken);

        if (!attendances.Any())
        {
            // Return empty response instead of error when no data is found
            var emptyResponse = new AbsenceReportResponse
            {
                StartDate = query.StartDate,
                EndDate = query.EndDate,
                ReportType = query.ReportType,
                Statistics = new AbsenceStatistics
                {
                    TotalEmployees = 0,
                    EmployeesWithAbsences = 0,
                    TotalAbsenceDays = 0,
                    AverageAbsenceDays = 0,
                    FrequentAbsences = 0,
                    AbsenceRate = 0,
                    AbsenceTypeDistribution = new Dictionary<AttendanceStatus, int>()
                },
                EmployeeSummaries = new List<EmployeeAbsenceSummary>(),
                DepartmentSummaries = new List<DepartmentAbsenceSummary>(),
                GeneratedAt = dateTimeProvider.Now
            };

            return emptyResponse;
        }

        // Calculate statistics
        AbsenceStatistics statistics = CalculateAbsenceStatistics(attendances);

        // Generate employee summaries
        List<EmployeeAbsenceSummary> employeeSummaries = GenerateEmployeeAbsenceSummaries(attendances);

        // Generate department summaries
        List<DepartmentAbsenceSummary> departmentSummaries = GenerateDepartmentAbsenceSummaries(attendances);

        var response = new AbsenceReportResponse
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

    private static AbsenceStatistics CalculateAbsenceStatistics(List<Domain.Entities.Attendance.Attendance> attendances)
    {
        IEnumerable<Guid> employeeIds = attendances.Select(a => a.EmployeeId).Distinct();
        int totalEmployees = employeeIds.Count();
        int employeesWithAbsences = totalEmployees;
        int totalAbsenceDays = attendances.Count;
        decimal averageAbsenceDays = totalEmployees > 0 ? (decimal)totalAbsenceDays / totalEmployees : 0;

        // Calculate frequent absences (more than 5 days in period)
        int frequentAbsences = attendances
            .GroupBy(a => a.EmployeeId)
            .Count(g => g.Count() > 5);

        // Calculate absence rate
        decimal absenceRate = totalAbsenceDays > 0 ? (decimal)totalAbsenceDays / attendances.Count * 100 : 0;

        // Calculate absence type distribution
        var absenceTypeDistribution = attendances
            .GroupBy(a => a.Status)
            .ToDictionary(g => g.Key, g => g.Count());

        return new AbsenceStatistics
        {
            TotalEmployees = totalEmployees,
            EmployeesWithAbsences = employeesWithAbsences,
            TotalAbsenceDays = totalAbsenceDays,
            AverageAbsenceDays = averageAbsenceDays,
            FrequentAbsences = frequentAbsences,
            AbsenceRate = absenceRate,
            AbsenceTypeDistribution = absenceTypeDistribution
        };
    }

    private static List<EmployeeAbsenceSummary> GenerateEmployeeAbsenceSummaries(
        List<Domain.Entities.Attendance.Attendance> attendances)
    {
        var summaries = attendances
            .GroupBy(a => a.EmployeeId)
            .Select(g => new EmployeeAbsenceSummary
            {
                EmployeeId = g.Key,
                FullName = g.First().Employee.FullName ?? string.Empty,
                DepartmentName = g.First().Employee.OrganizationalUnit?.UnitName ?? "Unknown",
                AbsenceDays = g.Count(),
                TotalAbsenceHours = g.Sum(a => a.WorkingMinutes ?? 0) / 60.0m, // Convert minutes to hours
                AverageAbsenceDuration = g.Any() ? g.Sum(a => a.WorkingMinutes ?? 0) / 60.0m / g.Count() : 0,
                MostFrequentAbsenceDay = CalculateMostFrequentAbsenceDay(g.ToList()),
                AbsenceRate = g.Any() ? (decimal)g.Count() / g.Count() * 100 : 0, // Placeholder calculation
                AbsenceTypeCount = g.GroupBy(a => a.Status).ToDictionary(gt => gt.Key, gt => gt.Count())
            })
            .ToList();

        return summaries;
    }

    private static List<DepartmentAbsenceSummary> GenerateDepartmentAbsenceSummaries(
        List<Domain.Entities.Attendance.Attendance> attendances)
    {
        var summaries = attendances
            .GroupBy(a => a.Employee.OrganizationalUnitId)
            .Select(g => new DepartmentAbsenceSummary
            {
                DepartmentId = g.Key ?? Guid.Empty,
                DepartmentName = g.First().Employee.OrganizationalUnit?.UnitName ?? "Unknown",
                TotalEmployees = g.Select(a => a.EmployeeId).Distinct().Count(),
                EmployeesWithAbsences = g.Select(a => a.EmployeeId).Distinct().Count(),
                TotalAbsenceDays = g.Count(),
                AverageAbsenceDays = g.Select(a => a.EmployeeId).Distinct().Any()
                    ? (decimal)g.Count() / g.Select(a => a.EmployeeId).Distinct().Count()
                    : 0,
                AbsenceRate = g.Select(a => a.EmployeeId).Distinct().Any()
                    ? (decimal)g.Select(a => a.EmployeeId).Distinct().Count() / g.Select(a => a.EmployeeId).Distinct().Count() * 100
                    : 0,
                AbsenceTypeDistribution = g.GroupBy(a => a.Status).ToDictionary(gt => gt.Key, gt => gt.Count())
            })
            .ToList();

        return summaries;
    }

    private static int CalculateMostFrequentAbsenceDay(List<Domain.Entities.Attendance.Attendance> attendances)
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
