using Application.Abstractions.Messaging;
using Domain.Enums;

namespace Application.Features.Organizations.Shifts.Update;

public sealed class UpdateShiftCommand : ICommand<bool>
{
    public Guid ShiftId { get; set; }

    public string Name { get; set; } = string.Empty;
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public ShiftType? ShiftType { get; set; }
    public bool? IsActive { get; set; }
    public string? Description { get; set; }
    public int? GracePeriodMinutes { get; set; }
    public int? MaxLateMinutes { get; set; }
    public bool AllowEarlyCheckIn { get; set; }
    public bool AllowLateCheckOut { get; set; }
}
