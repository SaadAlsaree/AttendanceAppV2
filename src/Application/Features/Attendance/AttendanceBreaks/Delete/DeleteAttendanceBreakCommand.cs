using Application.Abstractions.Messaging;

namespace Application.Attendance.AttendanceBreaks.Delete;

public sealed class DeleteAttendanceBreakCommand : ICommand<bool>
{
    public Guid AttendanceBreakId { get; set; }
}
