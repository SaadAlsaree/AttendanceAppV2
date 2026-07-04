using FluentValidation;

namespace Application.Features.Organizations.EmployeeWeeklyShifts.Get;

public class GetEmployeeWeeklyShiftsQueryValidator : AbstractValidator<GetEmployeeWeeklyShiftsQuery>
{
    public GetEmployeeWeeklyShiftsQueryValidator()
    {
        RuleFor(q => q.Page).GreaterThan(0).WithMessage("Page must be greater than 0");
        RuleFor(q => q.PageSize).GreaterThan(0).LessThanOrEqualTo(100).WithMessage("Page size must be between 1 and 100");
    }
}
