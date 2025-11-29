using Application.Abstractions.Messaging;
using Domain.Enums;

namespace Application.Attendance.Reports.GetComprehensiveAttendanceReport;

public sealed class GetComprehensiveAttendanceReportQuery : IQuery<ComprehensiveAttendanceReportResponse>
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? OrganizationalUnitId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? ManagerId { get; set; }
    public ReportType ReportType { get; set; }
    public string? GroupBy { get; set; }
    public bool IncludeHourlyLeaves { get; set; } = true;
    public bool IncludeOvertime { get; set; } = true;
    public bool IncludeLateDetails { get; set; } = true;
    public bool IncludeEarlyDepartures { get; set; } = true;
    public ExportFormat? ExportFormat { get; set; }
}

public sealed class ComprehensiveAttendanceReportResponse
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public ReportType ReportType { get; set; }
    public ComprehensiveAttendanceStatistics Statistics { get; set; } = new();
    public List<ComprehensiveEmployeeSummary> EmployeeSummaries { get; set; } = new();
    public List<ComprehensiveDepartmentSummary> DepartmentSummaries { get; set; } = new();
    public List<DailyAttendanceSummary> DailySummaries { get; set; } = new();
    public string? ExportFileUrl { get; set; }
    public DateTime GeneratedAt { get; set; }
}

public sealed class ComprehensiveAttendanceStatistics
{
    // إحصائيات عامة
    public int TotalEmployees { get; set; }
    public int TotalWorkingDays { get; set; }
    public int TotalAttendanceRecords { get; set; }
    public int TotalExpectedWorkingDays { get; set; }

    // إحصائيات الحضور
    public int PresentEmployees { get; set; }
    public int AbsentEmployees { get; set; }
    public int OnLeaveEmployees { get; set; }
    public decimal AverageAttendanceRate { get; set; }

    // إحصائيات التأخير
    public int LateArrivals { get; set; }
    public decimal TotalLateMinutes { get; set; }
    public decimal AverageLateMinutes { get; set; }
    public decimal LateArrivalRate { get; set; }

    // إحصائيات الانصراف المبكر
    public int EarlyDepartures { get; set; }
    public decimal TotalEarlyDepartureMinutes { get; set; }
    public decimal AverageEarlyDepartureMinutes { get; set; }
    public decimal EarlyDepartureRate { get; set; }

    // إحصائيات العمل الإضافي
    public int OvertimeEmployees { get; set; }
    public decimal TotalOvertimeHours { get; set; }
    public decimal AverageOvertimeHours { get; set; }
    public decimal OvertimeRate { get; set; }

    // إحصائيات الإجازات الساعية
    public int HourlyLeaveEmployees { get; set; }
    public decimal TotalHourlyLeaveHours { get; set; }
    public decimal AverageHourlyLeaveHours { get; set; }
    public int TotalHourlyLeaveRequests { get; set; }

    // إحصائيات ساعات العمل
    public decimal TotalWorkingHours { get; set; }
    public decimal AverageWorkingHours { get; set; }
    public decimal TotalBreakHours { get; set; }
    public decimal AverageBreakHours { get; set; }
}

public sealed class ComprehensiveEmployeeSummary
{
    public Guid EmployeeId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;

    // إحصائيات الحضور
    public int TotalWorkingDays { get; set; }
    public int ExpectedWorkingDays { get; set; }
    public int PresentDays { get; set; }
    public int AbsentDays { get; set; }
    public int OnLeaveDays { get; set; }
    public decimal AttendanceRate { get; set; }

    // إحصائيات التأخير
    public int LateArrivals { get; set; }
    public decimal TotalLateMinutes { get; set; }
    public decimal AverageLateMinutes { get; set; }
    public decimal LateArrivalRate { get; set; }

    // إحصائيات الانصراف المبكر
    public int EarlyDepartures { get; set; }
    public decimal TotalEarlyDepartureMinutes { get; set; }
    public decimal AverageEarlyDepartureMinutes { get; set; }
    public decimal EarlyDepartureRate { get; set; }

    // إحصائيات العمل الإضافي
    public int OvertimeDays { get; set; }
    public decimal TotalOvertimeHours { get; set; }
    public decimal AverageOvertimeHours { get; set; }
    public decimal OvertimeRate { get; set; }

    // إحصائيات الإجازات الساعية
    public int HourlyLeaveDays { get; set; }
    public decimal TotalHourlyLeaveHours { get; set; }
    public decimal AverageHourlyLeaveHours { get; set; }
    public int HourlyLeaveRequests { get; set; }

    // إحصائيات ساعات العمل
    public decimal TotalWorkingHours { get; set; }
    public decimal AverageWorkingHours { get; set; }
    public decimal TotalBreakHours { get; set; }
    public decimal AverageBreakHours { get; set; }

    // أداء عام
    public decimal PerformanceScore { get; set; }
    public string PerformanceLevel { get; set; } = string.Empty; // ممتاز، جيد، مقبول، ضعيف
}

public sealed class ComprehensiveDepartmentSummary
{
    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public int TotalEmployees { get; set; }

    // إحصائيات الحضور
    public int PresentEmployees { get; set; }
    public int AbsentEmployees { get; set; }
    public int OnLeaveEmployees { get; set; }
    public decimal AverageAttendanceRate { get; set; }

    // إحصائيات التأخير
    public int LateArrivals { get; set; }
    public decimal TotalLateMinutes { get; set; }
    public decimal AverageLateMinutes { get; set; }
    public decimal LateArrivalRate { get; set; }

    // إحصائيات الانصراف المبكر
    public int EarlyDepartures { get; set; }
    public decimal TotalEarlyDepartureMinutes { get; set; }
    public decimal AverageEarlyDepartureMinutes { get; set; }
    public decimal EarlyDepartureRate { get; set; }

    // إحصائيات العمل الإضافي
    public int OvertimeEmployees { get; set; }
    public decimal TotalOvertimeHours { get; set; }
    public decimal AverageOvertimeHours { get; set; }
    public decimal OvertimeRate { get; set; }

    // إحصائيات الإجازات الساعية
    public int HourlyLeaveEmployees { get; set; }
    public decimal TotalHourlyLeaveHours { get; set; }
    public decimal AverageHourlyLeaveHours { get; set; }
    public int TotalHourlyLeaveRequests { get; set; }

    // إحصائيات ساعات العمل
    public decimal TotalWorkingHours { get; set; }
    public decimal AverageWorkingHours { get; set; }
    public decimal TotalBreakHours { get; set; }
    public decimal AverageBreakHours { get; set; }

    // أداء القسم
    public decimal DepartmentPerformanceScore { get; set; }
    public string DepartmentPerformanceLevel { get; set; } = string.Empty;
}

public sealed class DailyAttendanceSummary
{
    public DateTime Date { get; set; }
    public string DayName { get; set; } = string.Empty;
    public int TotalEmployees { get; set; }
    public int ExpectedEmployees { get; set; }

    // إحصائيات الحضور
    public int PresentEmployees { get; set; }
    public int AbsentEmployees { get; set; }
    public int OnLeaveEmployees { get; set; }
    public decimal AttendanceRate { get; set; }

    // إحصائيات التأخير
    public int LateArrivals { get; set; }
    public decimal TotalLateMinutes { get; set; }
    public decimal AverageLateMinutes { get; set; }

    // إحصائيات الانصراف المبكر
    public int EarlyDepartures { get; set; }
    public decimal TotalEarlyDepartureMinutes { get; set; }
    public decimal AverageEarlyDepartureMinutes { get; set; }

    // إحصائيات العمل الإضافي
    public int OvertimeEmployees { get; set; }
    public decimal TotalOvertimeHours { get; set; }
    public decimal AverageOvertimeHours { get; set; }

    // إحصائيات الإجازات الساعية
    public int HourlyLeaveEmployees { get; set; }
    public decimal TotalHourlyLeaveHours { get; set; }
    public int HourlyLeaveRequests { get; set; }

    // إحصائيات ساعات العمل
    public decimal TotalWorkingHours { get; set; }
    public decimal AverageWorkingHours { get; set; }
    public decimal TotalBreakHours { get; set; }
    public decimal AverageBreakHours { get; set; }
}
