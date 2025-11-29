using SharedKernel;

namespace Domain.Entities.Organizations;

public sealed record EmployeeCreatedDomainEvent(Guid Id, string EmployeeNumber) : IDomainEvent;
