using Application.Abstractions.Messaging;

namespace Application.Attendance.Leaves.Approve;

public sealed class ApproveLeaveCommand : ICommand<Guid>
{
    public Guid LeaveId { get; set; }
    public Guid ApprovedBy { get; set; }
    public string? ApprovalNotes { get; set; }
}
