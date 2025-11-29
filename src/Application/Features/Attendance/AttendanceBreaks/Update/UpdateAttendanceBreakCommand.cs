using Application.Abstractions.Messaging;
using Application.Attendance.AttendanceBreaks.Get;
using Domain.Enums;

namespace Application.Attendance.AttendanceBreaks.Update;

public sealed class UpdateAttendanceBreakCommand : ICommand<AttendanceBreakResponse>
{
    public Guid AttendanceBreakId { get; set; }
    public BreakType? BreakType { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Notes { get; set; }
}
