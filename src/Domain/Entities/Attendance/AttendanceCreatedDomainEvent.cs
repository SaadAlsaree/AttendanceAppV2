using SharedKernel;

namespace Domain.Entities.Attendance;

public sealed record AttendanceCreatedDomainEvent(Guid AttendanceId, Guid EmployeeId, DateTime Date) : IDomainEvent;
