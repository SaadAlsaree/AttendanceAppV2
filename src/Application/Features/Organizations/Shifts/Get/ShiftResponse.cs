using Domain.Enums;

namespace Application.Features.Organizations.Shifts.Get;

public class ShiftResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; }
    public string Description { get; set; }
    public ShiftType ShiftType { get; set; }
    public string ShiftTypeName => ShiftType.ToString();
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    public int? GracePeriodMinutes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
