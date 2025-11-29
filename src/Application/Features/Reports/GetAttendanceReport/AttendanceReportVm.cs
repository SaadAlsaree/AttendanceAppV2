using Domain.Enums;

namespace Application.Features.Reports.GetAttendanceReport;

public sealed class AttendanceReportVm
{
    public Guid OrganizationalUnitId { get; set; }
    public string OrganizationalUnitName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime GeneratedAt { get; set; }

    // إحصائيات عامة
    public GeneralStatistics GeneralStats { get; set; } = new();

    // إحصائيات الشفتات
    public List<ShiftStatistics> ShiftStats { get; set; } = new();

    // إحصائيات الوحدات الفرعية
    public List<SubUnitStatistics> SubUnitStats { get; set; } = new();

    // إحصائيات الإجازات
    public LeaveStatistics LeaveStats { get; set; } = new();
}

public sealed class GeneralStatistics
{
    public int TotalEmployees { get; set; }
    public int TotalShifts { get; set; }
    public int TotalPresent { get; set; }
    public int TotalAbsent { get; set; }
    public int TotalLate { get; set; }
    public int TotalEarlyLeave { get; set; }
    public int TotalOvertime { get; set; }
    public int TotalNoCheckIn { get; set; }
    public int TotalNoCheckOut { get; set; }
    public int TotalOnLeave { get; set; }

    // النسب المئوية
    public decimal AttendanceRate { get; set; }
    public decimal AbsenceRate { get; set; }
    public decimal LateRate { get; set; }
    public decimal EarlyLeaveRate { get; set; }
    public decimal OvertimeRate { get; set; }
    public decimal LeaveRate { get; set; }
}

public sealed class ShiftStatistics
{
    public Guid ShiftId { get; set; }
    public string ShiftName { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int TotalEmployees { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public int LateCount { get; set; }
    public int EarlyLeaveCount { get; set; }
    public int OvertimeCount { get; set; }
    public int NoCheckInCount { get; set; }
    public int NoCheckOutCount { get; set; }
    public int OnLeaveCount { get; set; }

    // النسب المئوية
    public decimal AttendanceRate { get; set; }
    public decimal AbsenceRate { get; set; }
    public decimal LateRate { get; set; }
    public decimal EarlyLeaveRate { get; set; }
    public decimal OvertimeRate { get; set; }
    public decimal LeaveRate { get; set; }
}

public sealed class SubUnitStatistics
{
    public Guid UnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public string UnitCode { get; set; } = string.Empty;
    public int TotalEmployees { get; set; }
    public int TotalShifts { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public int LateCount { get; set; }
    public int EarlyLeaveCount { get; set; }
    public int OvertimeCount { get; set; }
    public int NoCheckInCount { get; set; }
    public int NoCheckOutCount { get; set; }
    public int OnLeaveCount { get; set; }

    // النسب المئوية
    public decimal AttendanceRate { get; set; }
    public decimal AbsenceRate { get; set; }
    public decimal LateRate { get; set; }
    public decimal EarlyLeaveRate { get; set; }
    public decimal OvertimeRate { get; set; }
    public decimal LeaveRate { get; set; }
}

public sealed class LeaveStatistics
{
    public int TotalLeaves { get; set; }
    public int ApprovedLeaves { get; set; }
    public int PendingLeaves { get; set; }
    public int RejectedLeaves { get; set; }

    // إحصائيات حسب نوع الإجازة
    public List<LeaveTypeStatistics> LeaveTypeStats { get; set; } = new();

    // النسب المئوية
    public decimal ApprovalRate { get; set; }
    public decimal RejectionRate { get; set; }
    public decimal PendingRate { get; set; }
}

public sealed class LeaveTypeStatistics
{
    public LeaveType LeaveType { get; set; }
    public string LeaveTypeName { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}
