using FluentValidation;
using Domain.Enums;

namespace Application.Attendance.Leaves.Create;

public class CreateLeaveCommandValidator : AbstractValidator<CreateLeaveCommand>
{
    public CreateLeaveCommandValidator()
    {
        RuleFor(c => c.EmployeeId)
            .NotEmpty()
            .WithMessage("Employee ID is required");

        RuleFor(c => c.LeaveType)
            .IsInEnum()
            .WithMessage("Leave type must be a valid value");

        RuleFor(c => c.StartDate)
            .NotEmpty()
            .WithMessage("Start date is required");

        RuleFor(c => c.EndDate)
            .NotEmpty()
            .WithMessage("End date is required");

        RuleFor(c => c.EndDate)
            .GreaterThanOrEqualTo(c => c.StartDate)
            .WithMessage("End date must be after or equal to start date");

        RuleFor(c => c.Reason)
            .NotEmpty()
            .WithMessage("Reason is required")
            .MaximumLength(500)
            .WithMessage("Reason cannot exceed 500 characters");

        RuleFor(c => c.Notes)
            .MaximumLength(500)
            .When(c => !string.IsNullOrEmpty(c.Notes))
            .WithMessage("Notes cannot exceed 500 characters");
    }
}
