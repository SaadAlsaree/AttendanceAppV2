
namespace Application.Features.Attendance.AttendanceSchedules.shared;

public sealed class ScheduleDayResponse
{
    public Guid Id { get; set; }
    public Guid AttendanceScheduleId { get; set; }
    public DateOnly ScheduleDayDate { get; set; }
    public Guid ShiftId { get; set; }
    public string ShiftName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
}
