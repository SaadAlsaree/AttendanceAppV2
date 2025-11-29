using Domain.Common;
using Domain.Enums;

namespace Domain.Entities.Organizations;

public sealed class Shift : AuditableEntity<Guid>
{
    public string Name { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public ShiftType ShiftType { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
    public int? GracePeriodMinutes { get; set; }
    public int? MaxLateMinutes { get; set; }
    public bool AllowEarlyCheckIn { get; set; }
    public bool AllowLateCheckOut { get; set; }

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();

}
