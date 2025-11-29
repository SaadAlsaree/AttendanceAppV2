using FluentValidation;

namespace Application.Features.Organizations.Shifts.Create;

public class CreateShiftCommandValidator : AbstractValidator<CreateShiftCommand>
{
    public CreateShiftCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(100).WithMessage("Shift name is required and cannot exceed 100 characters");
        RuleFor(c => c.ShiftType).IsInEnum().WithMessage("A valid shift type is required");
        RuleFor(c => c.StartTime).NotNull().WithMessage("Start time is required");
        RuleFor(c => c.EndTime).NotNull().WithMessage("End time is required");

    }
}
