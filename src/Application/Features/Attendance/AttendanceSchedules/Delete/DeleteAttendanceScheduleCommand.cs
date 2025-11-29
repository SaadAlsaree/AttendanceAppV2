using Application.Abstractions.Messaging;
using SharedKernel;

namespace Application.Attendance.AttendanceSchedules.Delete;

public sealed class DeleteAttendanceScheduleCommand : ICommand<bool>
{
    public Guid AttendanceScheduleId { get; set; }
}
