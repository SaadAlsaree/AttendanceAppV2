using Application.Abstractions.Messaging;

namespace Application.Attendance.Approve;

public sealed class ApproveAttendanceCommand : ICommand<AttendanceResponse>
{
    public Guid AttendanceId { get; set; }
    public Guid ApprovedBy { get; set; }
    public string? ApprovalNotes { get; set; }
}
