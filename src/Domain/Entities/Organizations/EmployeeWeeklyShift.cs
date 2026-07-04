using Domain.Common;

namespace Domain.Entities.Organizations;

public sealed class EmployeeWeeklyShift : AuditableEntity<Guid>
{
    public Guid EmployeeId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public Guid ShiftId { get; set; }

    // Navigation Properties
    public Employee Employee { get; set; } = default!;
    public Shift Shift { get; set; } = default!;
}
