using Application.Abstractions.Messaging;

namespace Application.Attendance.Leaves.GetById;

public sealed class GetLeaveByIdQuery : IQuery<LeaveResponse>
{
    public Guid LeaveId { get; set; }
}
