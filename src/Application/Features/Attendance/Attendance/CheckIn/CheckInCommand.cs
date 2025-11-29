using Application.Abstractions.Messaging;
using Domain.Entities.Attendance;
using Domain.Enums;

namespace Application.Attendance.CheckIn;

public sealed class CheckInCommand : ICommand<AttendanceResponse>
{
    public Guid AttendanceLogId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid OrganizationId { get; set; }
    public LogMethod? CheckInMethod { get; set; }
    public LogMethod? CheckOutMethod { get; set; }
    public string? Notes { get; set; }
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
