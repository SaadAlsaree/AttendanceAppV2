using Domain.Entities.Attendance;

namespace Application.Attendance.AttendanceLogs.GetById;

public sealed class AttendanceLogResponse
{
    public Guid Id { get; set; }
    public DateTime DateTimeAttend { get; set; }
    public string CardNo { get; set; } = string.Empty;
    public string EmpID { get; set; } = string.Empty;
    public DateOnly DateWork { get; set; }
    public TimeSpan? TimeAttend { get; set; }  // Assuming this stores only time
    public int Direct { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceNo { get; set; } = string.Empty;
    public string EmpName { get; set; } = string.Empty;
    // Navigation Properties
    public EmployeeResponse? Employee { get; set; }

}

public sealed class EmployeeResponse
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string RFID { get; set; } = string.Empty;
    public string OrganizationalUnitId { get; set; } = string.Empty;
    public string OrganizationalUnitName { get; set; } = string.Empty;
    public string OrganizationalUnitCode { get; set; } = string.Empty;
}
