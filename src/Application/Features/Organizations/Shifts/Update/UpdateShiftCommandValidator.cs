using FluentValidation;

namespace Application.Features.Organizations.Shifts.Update;

public class UpdateShiftCommandValidator : AbstractValidator<UpdateShiftCommand>
{
    public UpdateShiftCommandValidator()
    {
        RuleFor(c => c.ShiftId).NotEmpty().WithMessage("Shift ID is required");

        RuleFor(c => c.Name)
            .MaximumLength(100)
            .When(c => !string.IsNullOrEmpty(c.Name))
            .WithMessage("Shift name cannot exceed 100 characters");

        RuleFor(c => c.ShiftType)
            .IsInEnum()
            .When(c => c.ShiftType.HasValue)
            .WithMessage("A valid shift type is required");

        RuleFor(c => c.EndTime)
            .GreaterThan(c => c.StartTime)
            .WithMessage("End time must be after start time");

        RuleFor(c => c.GracePeriodMinutes)
            .GreaterThanOrEqualTo(0)
            .When(c => c.GracePeriodMinutes.HasValue)
            .WithMessage("Grace period minutes must be zero or positive");
    }
}
