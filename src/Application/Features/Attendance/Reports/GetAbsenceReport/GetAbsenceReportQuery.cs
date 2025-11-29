using Application.Abstractions.Messaging;
using Domain.Enums;

namespace Application.Attendance.Reports.GetAbsenceReport;

public sealed class GetAbsenceReportQuery : IQuery<AbsenceReportResponse>
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? OrganizationalUnitId { get; set; }
    public Guid? EmployeeId { get; set; }
    public AttendanceStatus? AbsenceType { get; set; }
    public ReportType ReportType { get; set; }
    public string? GroupBy { get; set; }
    public bool IncludePatterns { get; set; } = true;
    public int? MinAbsenceDays { get; set; }
    public ExportFormat? ExportFormat { get; set; }
}

public sealed class AbsenceReportResponse
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public ReportType ReportType { get; set; }
    public AbsenceStatistics Statistics { get; set; } = new();
    public List<EmployeeAbsenceSummary> EmployeeSummaries { get; set; } = new();
    public List<DepartmentAbsenceSummary> DepartmentSummaries { get; set; } = new();
    public string? ExportFileUrl { get; set; }
    public DateTime GeneratedAt { get; set; }
}

public sealed class AbsenceStatistics
{
    public int TotalEmployees { get; set; }
    public int EmployeesWithAbsences { get; set; }
    public int TotalAbsenceDays { get; set; }
    public decimal AverageAbsenceDays { get; set; }
    public int FrequentAbsences { get; set; } // More than 5 days in period
    public decimal AbsenceRate { get; set; } // Percentage of days with absences
    public Dictionary<AttendanceStatus, int> AbsenceTypeDistribution { get; set; } = new();
}

public sealed class EmployeeAbsenceSummary
{
    public Guid EmployeeId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public int AbsenceDays { get; set; }
    public decimal TotalAbsenceHours { get; set; }
    public decimal AverageAbsenceDuration { get; set; }
    public int MostFrequentAbsenceDay { get; set; } // Day of week with most absences
    public decimal AbsenceRate { get; set; } // Percentage of days with absences
    public Dictionary<AttendanceStatus, int> AbsenceTypeCount { get; set; } = new();
}

public sealed class DepartmentAbsenceSummary
{
    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public int TotalEmployees { get; set; }
    public int EmployeesWithAbsences { get; set; }
    public int TotalAbsenceDays { get; set; }
    public decimal AverageAbsenceDays { get; set; }
    public decimal AbsenceRate { get; set; } // Percentage of employees with absences
    public Dictionary<AttendanceStatus, int> AbsenceTypeDistribution { get; set; } = new();
}
