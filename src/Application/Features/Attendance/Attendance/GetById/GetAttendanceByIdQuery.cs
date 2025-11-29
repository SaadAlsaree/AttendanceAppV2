using Application.Abstractions.Messaging;
using SharedKernel;

namespace Application.Attendance.GetById;

public sealed class GetAttendanceByIdQuery : IQuery<ApiResponse<AttendanceResponse>>
{
    public Guid AttendanceId { get; set; }
}
