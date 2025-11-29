using SharedKernel;

namespace Domain.Entities.Attendance;

public sealed record AttendanceLogVerifiedDomainEvent(Guid LogId, Guid EmployeeId, Guid VerifiedBy) : IDomainEvent;
