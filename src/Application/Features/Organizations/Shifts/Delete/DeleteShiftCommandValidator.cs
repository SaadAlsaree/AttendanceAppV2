using FluentValidation;

namespace Application.Features.Organizations.Shifts.Delete;

public class DeleteShiftCommandValidator : AbstractValidator<DeleteShiftCommand>
{
    public DeleteShiftCommandValidator()
    {
        RuleFor(c => c.ShiftId).NotEmpty().WithMessage("Shift ID is required");
    }
}
