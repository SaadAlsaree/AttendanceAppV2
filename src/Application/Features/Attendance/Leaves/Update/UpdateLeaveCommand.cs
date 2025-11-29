using Application.Abstractions.Messaging;
using Domain.Enums;

namespace Application.Attendance.Leaves.Update;

public sealed class UpdateLeaveCommand : ICommand<Guid>
{
    public Guid LeaveId { get; set; }
    public LeaveType? LeaveType { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Reason { get; set; }
    public string? EmergencyContact { get; set; }
    public string? Notes { get; set; }
}
