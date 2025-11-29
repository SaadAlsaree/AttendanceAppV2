using Application.Abstractions.Messaging;

namespace Application.Attendance.AttendanceLogs.GetById;

public sealed record GetAttendanceLogByIdQuery(Guid AttendanceLogId) : IQuery<AttendanceLogResponse>;
