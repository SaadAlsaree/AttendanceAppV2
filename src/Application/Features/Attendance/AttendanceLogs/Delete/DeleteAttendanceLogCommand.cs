using Application.Abstractions.Messaging;

namespace Application.Attendance.AttendanceLogs.Delete;

public sealed class DeleteAttendanceLogCommand : ICommand<bool>
{
    public Guid AttendanceLogId { get; set; }
}
