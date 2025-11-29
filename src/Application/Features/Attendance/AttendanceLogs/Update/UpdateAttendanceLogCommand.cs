using Application.Abstractions.Messaging;
using Application.Attendance.AttendanceLogs.GetById;
using Domain.Entities.Attendance;

namespace Application.Attendance.AttendanceLogs.Update;

public sealed class UpdateAttendanceLogCommand : ICommand<AttendanceLogResponse>
{
    public Guid AttendanceLogId { get; set; }
    public DateTime? DateTimeAttend { get; set; }
    public string? CardNo { get; set; }
    public string? EmpID { get; set; }
    public DateOnly? DateWork { get; set; }
    public TimeSpan? TimeAttend { get; set; }
    public int? Direct { get; set; }
    public string? DeviceName { get; set; }
    public string? DeviceNo { get; set; }
}
