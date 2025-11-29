using FluentValidation;
using Domain.Enums;

namespace Application.Attendance.Leaves.Update;

public class UpdateLeaveCommandValidator : AbstractValidator<UpdateLeaveCommand>
{
    public UpdateLeaveCommandValidator()
    {
        RuleFor(c => c.LeaveId)
            .NotEmpty()
            .WithMessage("Leave ID is required");

        RuleFor(c => c.LeaveType)
            .IsInEnum()
            .When(c => c.LeaveType.HasValue)
            .WithMessage("Leave type must be a valid value");

        RuleFor(c => c.StartDate)
            .NotEmpty()
            .When(c => c.StartDate.HasValue)
            .WithMessage("Start date is required when provided");

        RuleFor(c => c.EndDate)
            .NotEmpty()
            .When(c => c.EndDate.HasValue)
            .WithMessage("End date is required when provided");

        RuleFor(c => c.Reason)
            .MaximumLength(500)
            .When(c => !string.IsNullOrEmpty(c.Reason))
            .WithMessage("Reason cannot exceed 500 characters");

        RuleFor(c => c.Notes)
            .MaximumLength(500)
            .When(c => !string.IsNullOrEmpty(c.Notes))
            .WithMessage("Notes cannot exceed 500 characters");
    }
}
