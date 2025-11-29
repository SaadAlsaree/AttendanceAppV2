using Domain.Common;
using Domain.Entities.Organizations;
using Domain.Enums;

namespace Domain.Entities.Attendance;

public sealed class ScheduleIssue : AuditableEntity<Guid>
{
    public Guid AttendanceScheduleId { get; set; }
    public DateOnly Date { get; set; }
    public Guid ShiftId { get; set; } // The different shift to apply on this date
    public string Reason { get; set; } = string.Empty;
    public ExceptionType ExceptionType { get; set; }

    // Navigation Properties
    public AttendanceSchedule AttendanceSchedule { get; set; } = null!;
    public Shift Shift { get; set; } = null!;
}
