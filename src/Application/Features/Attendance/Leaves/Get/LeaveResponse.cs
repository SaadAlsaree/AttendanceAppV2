using Domain.Enums;

namespace Application.Attendance.Leaves.Get;

public sealed class LeaveResponse
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public LeaveType LeaveType { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public LeaveStatus Status { get; set; }
    public Guid? ApprovedBy { get; set; }
    public string? ApproverName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
}
