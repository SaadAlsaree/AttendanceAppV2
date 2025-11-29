using SharedKernel;

namespace Domain.Entities.Organizations;

public sealed record WorkLocationCreatedDomainEvent(Guid WorkLocationId, Guid OrganizationId, string Name) : IDomainEvent;
