using SharedKernel;

namespace Domain.Entities.Attendance;

public sealed record AttendanceLogRejectedDomainEvent(Guid LogId, Guid EmployeeId) : IDomainEvent;
