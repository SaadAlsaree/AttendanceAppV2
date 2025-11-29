using SharedKernel;

namespace Domain.Entities.Organizations;

public sealed record EmployeeManagerAssignedDomainEvent(Guid Id, Guid ManagerId) : IDomainEvent;
