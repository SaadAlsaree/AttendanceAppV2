using Domain.Common;
using Domain.Entities.Organizations;

namespace Domain.Entities.Attendance;

public sealed class ScheduleDay : AuditableEntity<Guid>
{
    public Guid AttendanceScheduleId { get; set; }
    public DateOnly ScheduleDayDate { get; set; }
    public Guid ShiftId { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }

    // Navigation Properties
    public AttendanceSchedule AttendanceSchedule { get; set; } = default!;
    public Shift Shift { get; set; } = default!;
}
