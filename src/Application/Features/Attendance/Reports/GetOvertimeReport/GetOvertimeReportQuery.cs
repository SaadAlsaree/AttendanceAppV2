using Application.Abstractions.Messaging;
using Domain.Enums;

namespace Application.Attendance.Reports.GetOvertimeReport;

public sealed class GetOvertimeReportQuery : IQuery<OvertimeReportResponse>
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? OrganizationalUnitId { get; set; }
    public Guid? EmployeeId { get; set; }
    public string? OvertimeType { get; set; }
    public ReportType ReportType { get; set; }
    public string? GroupBy { get; set; }
    public bool IncludeCosts { get; set; } = true;
    public int? MinOvertimeHours { get; set; }
    public ExportFormat? ExportFormat { get; set; }
}

public sealed class OvertimeReportResponse
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public ReportType ReportType { get; set; }
    public OvertimeStatistics Statistics { get; set; } = new();
    public List<EmployeeOvertimeSummary> EmployeeSummaries { get; set; } = new();
    public List<DepartmentOvertimeSummary> DepartmentSummaries { get; set; } = new();
    public string? ExportFileUrl { get; set; }
    public DateTime GeneratedAt { get; set; }
}

public sealed class OvertimeStatistics
{
    public int TotalEmployees { get; set; }
    public int EmployeesWithOvertime { get; set; }
    public decimal TotalOvertimeHours { get; set; }
    public decimal AverageOvertimeHours { get; set; }
    public decimal TotalOvertimeCost { get; set; }
    public decimal AverageOvertimeCost { get; set; }
    public int RegularOvertimeCount { get; set; }
    public int HolidayOvertimeCount { get; set; }
    public int WeekendOvertimeCount { get; set; }
}

public sealed class EmployeeOvertimeSummary
{
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public decimal TotalOvertimeHours { get; set; }
    public decimal RegularOvertimeHours { get; set; }
    public decimal HolidayOvertimeHours { get; set; }
    public decimal WeekendOvertimeHours { get; set; }
    public decimal OvertimeCost { get; set; }
    public int OvertimeDays { get; set; }
    public decimal AverageOvertimePerDay { get; set; }
}

public sealed class DepartmentOvertimeSummary
{
    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public int TotalEmployees { get; set; }
    public int EmployeesWithOvertime { get; set; }
    public decimal TotalOvertimeHours { get; set; }
    public decimal AverageOvertimeHours { get; set; }
    public decimal TotalOvertimeCost { get; set; }
    public decimal AverageOvertimeCost { get; set; }
}
