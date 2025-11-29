using Application.Abstractions.Messaging;

namespace Application.Attendance.Create;

public sealed class CreateAttendanceCommand : ICommand<Guid>
{
    public Guid EmployeeId { get; set; }
    public Guid OrganizationId { get; set; }
    public DateTime Date { get; set; }
    public Guid? ShiftId { get; set; }
    public Guid? AttendanceScheduleId { get; set; }
    public string? Notes { get; set; }
}
