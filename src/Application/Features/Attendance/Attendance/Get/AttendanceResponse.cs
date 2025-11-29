using Domain.Enums;

namespace Application.Attendance.Get;

public sealed class AttendanceResponse
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid OrganizationId { get; set; }
    public string? OrganizationalName { get; set; }
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
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public LeaveType LeaveType { get; set; }
    public Guid LeaveId { get; set; }
    // Navigation properties
    public string? FullName { get; set; }
    public string? Code { get; set; }
    public string? ShiftName { get; set; }
    public string? ApproverName { get; set; }
}
