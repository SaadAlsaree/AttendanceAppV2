using Domain.Enums;

namespace Application.Attendance.AttendanceBreaks.Get;

public sealed class AttendanceBreakResponse
{
    public Guid Id { get; set; }
    public Guid AttendanceId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int DurationMinutes { get; set; }
    public BreakType BreakType { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
    public string? AttendanceDate { get; set; }
}
