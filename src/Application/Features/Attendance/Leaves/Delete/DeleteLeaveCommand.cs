using Application.Abstractions.Messaging;

namespace Application.Attendance.Leaves.Delete;

public sealed class DeleteLeaveCommand : ICommand<bool>
{
    public Guid LeaveId { get; set; }
}
