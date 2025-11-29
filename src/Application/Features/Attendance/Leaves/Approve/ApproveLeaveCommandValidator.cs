using FluentValidation;

namespace Application.Attendance.Leaves.Approve;

public class ApproveLeaveCommandValidator : AbstractValidator<ApproveLeaveCommand>
{
    public ApproveLeaveCommandValidator()
    {
        RuleFor(c => c.LeaveId)
            .NotEmpty()
            .WithMessage("Leave ID is required");

        RuleFor(c => c.ApprovedBy)
            .NotEmpty()
            .WithMessage("Approver ID is required");

        RuleFor(c => c.ApprovalNotes)
            .MaximumLength(500)
            .When(c => !string.IsNullOrEmpty(c.ApprovalNotes))
            .WithMessage("Approval notes cannot exceed 500 characters");
    }
}
