using Domain.Common;
using Domain.Entities.Organizations;
using Domain.Enums;

namespace Domain.Entities.Attendance;

public sealed class AttendanceSchedule : AuditableEntity<Guid>
{
    public Guid EmployeeId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public ScheduleType ScheduleType { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }

    // Navigation Properties
    public Employee Employee { get; set; } = default!;

    public List<DateOnly> ExcludedDates { get; set; } = new List<DateOnly>();
    public ICollection<ScheduleIssue> Exceptions { get; set; } = new List<ScheduleIssue>();
    public ICollection<ScheduleDay> ScheduleDays { get; set; } = new List<ScheduleDay>();
    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
}
