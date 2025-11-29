using Application.Abstractions.Messaging;
using Domain.Enums;

namespace Application.Attendance.Leaves.Create;

public sealed class CreateLeaveCommand : ICommand<Guid>
{
    public Guid EmployeeId { get; set; }
    public LeaveType LeaveType { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid? ManagerId { get; set; }
    public string? EmergencyContact { get; set; }
    public string? Notes { get; set; }
}
