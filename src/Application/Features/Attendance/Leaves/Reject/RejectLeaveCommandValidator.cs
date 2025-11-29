using FluentValidation;

namespace Application.Attendance.Leaves.Reject;

public class RejectLeaveCommandValidator : AbstractValidator<RejectLeaveCommand>
{
    public RejectLeaveCommandValidator()
    {
        RuleFor(c => c.LeaveId)
            .NotEmpty()
            .WithMessage("Leave ID is required");

        RuleFor(c => c.RejectedBy)
            .NotEmpty()
            .WithMessage("RejectedBy (user) is required");

        RuleFor(c => c.RejectionReason)
            .NotEmpty()
            .WithMessage("Rejection reason is required")
            .MaximumLength(500)
            .WithMessage("Rejection reason cannot exceed 500 characters");

        RuleFor(c => c.RejectionNotes)
            .MaximumLength(500)
            .When(c => !string.IsNullOrEmpty(c.RejectionNotes))
            .WithMessage("Rejection notes cannot exceed 500 characters");
    }
}
