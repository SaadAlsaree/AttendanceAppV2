using SharedKernel;

namespace Domain.Entities.Attendance;

public sealed record AttendanceCheckedInDomainEvent(Guid AttendanceId, Guid EmployeeId, DateTime CheckInTime) : IDomainEvent;
