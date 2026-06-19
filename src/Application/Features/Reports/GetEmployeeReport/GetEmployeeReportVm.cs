namespace Application.Features.Reports.GetEmployeeReport;

public class GetEmployeeReportVm
{
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string? EmployeeCode { get; set; }
    public string OrganizationalUnitName { get; set; } = string.Empty;

    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public DateTime GeneratedAt { get; set; }

    // ملخص الفترة
    public int TotalDays { get; set; }
    public int PresentDays { get; set; }
    public int AbsentDays { get; set; }
    public int LateDays { get; set; }
    public int EarlyLeaveDays { get; set; }
    public int LeaveDays { get; set; }
    public double TotalOvertimeHours { get; set; }

    // تفصيل يومي
    public List<EmployeeReportDay> Days { get; set; } = new();
}

public sealed class EmployeeReportDay
{
    public DateTime Date { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public int LateMinutes { get; set; }
    public int EarlyLeaveMinutes { get; set; }
    public int OvertimeMinutes { get; set; }
    // غير مبصم: لا يوجد تسجيل دخول ولا خروج
    public bool IsNonFingerprinted { get; set; }
}
