using SharedKernel;

namespace Domain.Entities.Organizations;
public sealed record ShiftCreatedDomainEvent(Guid ShiftId) : IDomainEvent;
