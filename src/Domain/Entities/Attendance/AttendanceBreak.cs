using Domain.Common;
using Domain.Enums;

namespace Domain.Entities.Attendance;

public sealed class AttendanceBreak : AuditableEntity<Guid>
{
    public Guid AttendanceId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int DurationMinutes { get; set; }
    public BreakType BreakType { get; set; }
    public string? Notes { get; set; }

    // Navigation Properties
    public Attendance Attendance { get; set; } = null!;

}
