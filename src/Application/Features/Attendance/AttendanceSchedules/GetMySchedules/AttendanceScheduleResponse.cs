using Application.Features.Attendance.AttendanceSchedules.shared;
using Domain.Enums;

namespace Application.Attendance.AttendanceSchedules.GetMySchedules;

public sealed class AttendanceScheduleResponse
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? EmployeeName { get; set; }
    public string? EmployeeEmail { get; set; }
    public Guid? EmployeeOrganizationId { get; set; }
    public string? EmployeeOrganizationName { get; set; }
    public ScheduleType ScheduleType { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
    public List<DateOnly> ExcludedDates { get; set; } = new List<DateOnly>();
    public List<ScheduleDayResponse> ScheduleDays { get; set; } = new List<ScheduleDayResponse>();
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
}


