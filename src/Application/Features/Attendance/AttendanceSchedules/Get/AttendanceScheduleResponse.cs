using Application.Features.Attendance.AttendanceSchedules.shared;
using Domain.Enums;

namespace Application.Attendance.AttendanceSchedules.Get;

public sealed class AttendanceScheduleResponse
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public ScheduleType ScheduleType { get; set; }
    public string ScheduleTypeName { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
    public List<DateOnly> ExcludedDates { get; set; } = new List<DateOnly>();
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
    public List<ScheduleDayResponse> ScheduleDays { get; set; } = new List<ScheduleDayResponse>();
}



