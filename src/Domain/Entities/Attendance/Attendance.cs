using Domain.Common;
using Domain.Entities.Organizations;
using Domain.Enums;

namespace Domain.Entities.Attendance;

public sealed class Attendance : AuditableEntity<Guid>
{
    public Guid EmployeeId { get; set; }
    public Guid OrganizationId { get; set; }
    public DateTime Date { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public AttendanceStatus Status { get; set; }
    public Guid? ShiftId { get; set; }
    public int? WorkingMinutes { get; set; }
    public int? BreakMinutes { get; set; }
    public int? OvertimeMinutes { get; set; }
    public int? LateMinutes { get; set; }
    public int? EarlyLeaveMinutes { get; set; }
    public string? Notes { get; set; }
    public LogMethod? CheckInMethod { get; set; }
    public LogMethod? CheckOutMethod { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? AttendanceScheduleId { get; set; }

    // Navigation Properties
    public Employee Employee { get; set; } = null!;
    public Shift? Shift { get; set; }
    public AttendanceSchedule? AttendanceSchedule { get; set; }
    public List<AttendanceBreak> Breaks { get; set; } = new List<AttendanceBreak>();
}
