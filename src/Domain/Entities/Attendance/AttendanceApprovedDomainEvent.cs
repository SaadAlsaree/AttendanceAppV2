using SharedKernel;

namespace Domain.Entities.Attendance;

public sealed record AttendanceApprovedDomainEvent(Guid AttendanceId, Guid EmployeeId, Guid ApprovedBy) : IDomainEvent;
