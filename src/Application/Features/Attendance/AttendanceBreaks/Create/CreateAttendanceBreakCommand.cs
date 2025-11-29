using Application.Abstractions.Messaging;
using Domain.Enums;

namespace Application.Attendance.AttendanceBreaks.Create;

public sealed class CreateAttendanceBreakCommand : ICommand<Guid>
{
    public Guid AttendanceId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int? DurationMinutes { get; set; }
    public BreakType BreakType { get; set; }
    public string? Notes { get; set; }
}
