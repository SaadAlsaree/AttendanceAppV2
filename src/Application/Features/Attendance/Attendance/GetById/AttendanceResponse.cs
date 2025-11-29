using Domain.Enums;

namespace Application.Attendance.GetById;

public sealed class AttendanceResponse
{
    public Guid Id { get; set; }
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
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? AttendanceScheduleId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public string? FullName { get; set; }
    public string? Code { get; set; }
    public string? ShiftName { get; set; }
    public string? ApproverName { get; set; }
    public List<AttendanceLogDto> Logs { get; set; } = new List<AttendanceLogDto>();
}

public class AttendanceLogDto
{
    public Guid Id { get; set; }
    public Guid AttendanceId { get; set; }
    public int? Major { get; set; }
    public int? Minor { get; set; }
    public DateTime Time { get; set; }
    public string CardNo { get; set; } = string.Empty;
    public int? CardType { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? CardReaderNo { get; set; }
    public int? DoorNo { get; set; }
    public string? EmployeeNoString { get; set; }
    public int? SerialNo { get; set; }
    public string? UserType { get; set; }
    public string? CurrentVerifyMode { get; set; }
    public string? AttendanceStatus { get; set; }
    public string? Label { get; set; }
    public string? Mask { get; set; }
    public string? PictureURL { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
