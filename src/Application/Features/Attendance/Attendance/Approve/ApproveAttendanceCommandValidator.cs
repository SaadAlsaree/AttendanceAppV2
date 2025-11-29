using FluentValidation;

namespace Application.Attendance.Approve;

internal sealed class ApproveAttendanceCommandValidator : AbstractValidator<ApproveAttendanceCommand>
{
    public ApproveAttendanceCommandValidator()
    {
        RuleFor(x => x.AttendanceId)
            .NotEmpty()
            .WithMessage("Attendance ID is required.");

        RuleFor(x => x.ApprovedBy)
            .NotEmpty()
            .WithMessage("Approver ID is required.");
    }
}
