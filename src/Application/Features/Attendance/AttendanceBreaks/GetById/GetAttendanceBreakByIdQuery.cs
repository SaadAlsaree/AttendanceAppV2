using Application.Abstractions.Messaging;
using Application.Attendance.AttendanceBreaks.Get;

namespace Application.Attendance.AttendanceBreaks.GetById;

public sealed class GetAttendanceBreakByIdQuery : IQuery<AttendanceBreakResponse>
{
    public Guid AttendanceBreakId { get; set; }
}
