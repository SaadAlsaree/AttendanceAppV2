using SharedKernel;

namespace Domain.Entities.Organizations;

public sealed record EmployeeUpdatedDomainEvent(Guid Id, string EmployeeNumber) : IDomainEvent;
