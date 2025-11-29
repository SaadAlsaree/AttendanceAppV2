using SharedKernel;

namespace Domain.Entities.Devices;

public sealed record DeviceCreatedDomainEvent(Guid DeviceId, string UserName, string Password, string IpAddress) : IDomainEvent;
