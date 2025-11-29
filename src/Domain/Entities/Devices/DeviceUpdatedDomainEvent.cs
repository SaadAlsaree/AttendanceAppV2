using SharedKernel;

namespace Domain.Entities.Devices;

public sealed record DeviceUpdatedDomainEvent(Guid DeviceId, string Name) : IDomainEvent;
