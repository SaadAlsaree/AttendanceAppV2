using Application.Abstractions.Messaging;
using SharedKernel;

namespace Application.Attendance.AttendanceSchedules.GetById;

public sealed class GetAttendanceScheduleByIdQuery : IQuery<ApiResponse<AttendanceScheduleResponse>>
{
    public Guid AttendanceScheduleId { get; set; }
}
