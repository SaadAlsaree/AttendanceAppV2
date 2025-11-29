using SharedKernel;

namespace Domain.Entities.Organizations;

public sealed record HolidayCreatedDomainEvent(Holiday Holiday) : IDomainEvent;
