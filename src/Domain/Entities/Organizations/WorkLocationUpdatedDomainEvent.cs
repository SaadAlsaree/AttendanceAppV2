using SharedKernel;

namespace Domain.Entities.Organizations;

public sealed record WorkLocationUpdatedDomainEvent(Guid WorkLocationId, string Name) : IDomainEvent;
