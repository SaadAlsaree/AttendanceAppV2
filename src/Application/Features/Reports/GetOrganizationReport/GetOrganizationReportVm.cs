namespace Application.Features.Reports.GetOrganizationReport;

public class GetOrganizationReportVm
{
    public DateTime Date { get; set; }
    public DateTime GeneratedAt { get; set; }
    public int TotalEmployees { get; set; }
    public int TotalAttendances { get; set; }
    public int TotalNotAttendances { get; set; }
    public int TotalLate { get; set; }
    public int TotalLeaves { get; set; }
    public int TotalOvertime { get; set; }

    // ملخص الوحدات التنظيمية
    public List<UnitSummary> Units { get; set; } = new();

}

public sealed class UnitSummary
{
    public Guid UnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public string UnitCode { get; set; } = string.Empty;
    public Guid? ParentUnitId { get; set; }
    public string? ParentUnitName { get; set; }

    // إحصائيات الوحدة
    public int TotalEmployees { get; set; }
    public int TotalShifts { get; set; }
    public int TotalAttendances { get; set; }
    public int TotalNotAttendances { get; set; }
    public int TotalLate { get; set; }
    public int TotalLeaves { get; set; }
    public int TotalOvertime { get; set; }
    public List<EmployeeAttendanceDetail> EmployeeDetails { get; set; } = new();

}

public sealed class EmployeeAttendanceDetail
{
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public Guid OrganizationalUnitId { get; set; }
    public string OrganizationalUnitName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public bool IsLate { get; set; }
    public bool IsEarlyLeave { get; set; }
    public bool IsOnLeave { get; set; }
    public bool IsAbsent { get; set; }
    public TimeSpan? OvertimeDuration { get; set; }
}
