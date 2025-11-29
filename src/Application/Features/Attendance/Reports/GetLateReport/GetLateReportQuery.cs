using Application.Abstractions.Messaging;
using Domain.Enums;

namespace Application.Attendance.Reports.GetLateReport;

public sealed class GetLateReportQuery : IQuery<LateReportResponse>
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? OrganizationalUnitId { get; set; }
    public Guid? EmployeeId { get; set; }
    public int? MinLateMinutes { get; set; }
    public ReportType ReportType { get; set; }
    public string? GroupBy { get; set; }
    public bool IncludeTrends { get; set; } = true;
    public ExportFormat? ExportFormat { get; set; }
}

public sealed class LateReportResponse
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public ReportType ReportType { get; set; }
    public LateStatistics Statistics { get; set; } = new();
    public List<EmployeeLateSummary> EmployeeSummaries { get; set; } = new();
    public List<DepartmentLateSummary> DepartmentSummaries { get; set; } = new();
    public string? ExportFileUrl { get; set; }
    public DateTime GeneratedAt { get; set; }
}

public sealed class LateStatistics
{
    public int TotalEmployees { get; set; }
    public int EmployeesWithLateArrivals { get; set; }
    public int TotalLateArrivals { get; set; }
    public decimal AverageLateMinutes { get; set; }
    public decimal TotalLateMinutes { get; set; }
    public int FrequentLateArrivals { get; set; } // More than 3 times in period
    public decimal LateArrivalRate { get; set; } // Percentage of days with late arrivals
}

public sealed class EmployeeLateSummary
{
    public Guid EmployeeId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public int LateArrivalDays { get; set; }
    public decimal TotalLateMinutes { get; set; }
    public decimal AverageLateMinutes { get; set; }
    public int MostFrequentLateDay { get; set; } // Day of week with most late arrivals
    public decimal LateArrivalRate { get; set; } // Percentage of days with late arrivals
}

public sealed class DepartmentLateSummary
{
    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public int TotalEmployees { get; set; }
    public int EmployeesWithLateArrivals { get; set; }
    public int TotalLateArrivals { get; set; }
    public decimal AverageLateMinutes { get; set; }
    public decimal LateArrivalRate { get; set; } // Percentage of employees with late arrivals
}
