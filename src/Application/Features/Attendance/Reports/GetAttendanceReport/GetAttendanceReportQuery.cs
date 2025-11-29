using Application.Abstractions.Messaging;
using Domain.Enums;

namespace Application.Attendance.Reports.GetAttendanceReport;

public sealed class GetAttendanceReportQuery : IQuery<AttendanceReportResponse>
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? OrganizationalUnitId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? ManagerId { get; set; }
    public ReportType ReportType { get; set; }
    public string? GroupBy { get; set; }
    public bool IncludeBreaks { get; set; } = true;
    public bool IncludeOvertime { get; set; } = true;
    public ExportFormat? ExportFormat { get; set; }
}

public sealed class AttendanceReportResponse
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public ReportType ReportType { get; set; }
    public AttendanceStatistics Statistics { get; set; } = new();
    public List<EmployeeAttendanceSummary> EmployeeSummaries { get; set; } = new();
    public List<DepartmentAttendanceSummary> DepartmentSummaries { get; set; } = new();
    public string? ExportFileUrl { get; set; }
    public DateTime GeneratedAt { get; set; }
}

public sealed class AttendanceStatistics
{
    public int TotalEmployees { get; set; }
    public int PresentEmployees { get; set; }
    public int AbsentEmployees { get; set; }
    public int LateArrivals { get; set; }
    public int EarlyDepartures { get; set; }
    public decimal TotalOvertimeHours { get; set; }
    public decimal AverageAttendanceRate { get; set; }
}

public sealed class EmployeeAttendanceSummary
{
    public Guid EmployeeId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public int PresentDays { get; set; }
    public int AbsentDays { get; set; }
    public int LateArrivals { get; set; }
    public int EarlyDepartures { get; set; }
    public decimal TotalOvertimeHours { get; set; }
    public decimal AttendanceRate { get; set; }
}

public sealed class DepartmentAttendanceSummary
{
    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public int TotalEmployees { get; set; }
    public int PresentEmployees { get; set; }
    public int AbsentEmployees { get; set; }
    public decimal AverageAttendanceRate { get; set; }
    public decimal TotalOvertimeHours { get; set; }
}
