using Application.Abstractions.Messaging;

namespace Application.Attendance.Leaves.Reject;

public sealed class RejectLeaveCommand : ICommand<Guid>
{
    public Guid LeaveId { get; set; }
    public Guid RejectedBy { get; set; }
    public string RejectionReason { get; set; } = string.Empty;
    public string? RejectionNotes { get; set; }
}
