using Domain.Enums;
using SharedKernel;

namespace Domain.Entities.Devices;

public sealed record DeviceStatusChangedDomainEvent(Guid DeviceId, DeviceStatus Status, bool IsOnline) : IDomainEvent;
