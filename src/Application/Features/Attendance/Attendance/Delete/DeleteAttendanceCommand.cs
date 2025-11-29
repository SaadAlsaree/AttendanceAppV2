using Application.Abstractions.Messaging;

namespace Application.Attendance.Delete;

public sealed class DeleteAttendanceCommand : ICommand
{
    public Guid AttendanceId { get; set; }
}
