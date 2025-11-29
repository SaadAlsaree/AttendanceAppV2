using FluentValidation;

namespace Application.Features.Organizations.Shifts.Get;

public class GetShiftsQueryValidator : AbstractValidator<GetShiftsQuery>
{
    public GetShiftsQueryValidator()
    {
        RuleFor(q => q.Page).GreaterThan(0).WithMessage("Page must be greater than 0");
        RuleFor(q => q.PageSize).GreaterThan(0).LessThanOrEqualTo(100).WithMessage("Page size must be between 1 and 100");
        RuleFor(q => q.ShiftType).IsInEnum().When(q => q.ShiftType.HasValue).WithMessage("Invalid shift type");
    }
}
