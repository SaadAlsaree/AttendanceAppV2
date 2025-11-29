using Application.Abstractions.Messaging;
using Domain.Enums;

namespace Application.Attendance.Update;

public sealed class UpdateAttendanceCommand : ICommand<AttendanceResponse>
{
    public Guid AttendanceId { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public string? Notes { get; set; }
    public AttendanceStatus? Status { get; set; }
}
