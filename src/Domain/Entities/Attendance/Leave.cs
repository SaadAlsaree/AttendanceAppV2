using Domain.Common;
using Domain.Entities.Organizations;
using Domain.Enums;

namespace Domain.Entities.Attendance;

public sealed class Leave : AuditableEntity<Guid>
{
    public Guid EmployeeId { get; set; }
    public LeaveType LeaveType { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public LeaveStatus Status { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }

    // Navigation Properties
    public Employee Employee { get; set; } = null!;

}
