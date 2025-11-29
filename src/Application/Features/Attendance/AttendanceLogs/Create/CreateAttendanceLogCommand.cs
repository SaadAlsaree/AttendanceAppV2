using Application.Abstractions.Messaging;
using Domain.Entities.Attendance;

namespace Application.Attendance.AttendanceLogs.Create;

public sealed class CreateAttendanceLogCommand : ICommand<Guid>
{
    public DateTime DateTimeAttend { get; set; }
    public string CardNo { get; set; } = string.Empty;
    public string EmpID { get; set; } = string.Empty;
    public DateOnly DateWork { get; set; }
    public TimeSpan? TimeAttend { get; set; }  // Assuming this stores only time
    public int Direct { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceNo { get; set; } = string.Empty;
    public string EmpName { get; set; } = string.Empty;
}
