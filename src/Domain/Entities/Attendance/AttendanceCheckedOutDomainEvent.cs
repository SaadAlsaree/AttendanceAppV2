using SharedKernel;

namespace Domain.Entities.Attendance;

public sealed record AttendanceCheckedOutDomainEvent(Guid AttendanceId, Guid EmployeeId, DateTime CheckOutTime) : IDomainEvent;
