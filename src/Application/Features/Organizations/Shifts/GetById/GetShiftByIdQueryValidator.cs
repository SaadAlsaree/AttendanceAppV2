using FluentValidation;

namespace Application.Features.Organizations.Shifts.GetById;

public class GetShiftByIdQueryValidator : AbstractValidator<GetShiftByIdQuery>
{
    public GetShiftByIdQueryValidator()
    {
        RuleFor(q => q.ShiftId).NotEmpty().WithMessage("Shift ID is required");
    }
}
