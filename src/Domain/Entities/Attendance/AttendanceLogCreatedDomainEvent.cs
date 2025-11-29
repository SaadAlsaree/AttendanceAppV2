using SharedKernel;

namespace Domain.Entities.Attendance;

public sealed record AttendanceLogCreatedDomainEvent(Guid LogId, Guid EmployeeId, DateTime Time) : IDomainEvent;
