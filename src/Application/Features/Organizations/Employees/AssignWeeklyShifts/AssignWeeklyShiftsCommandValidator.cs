using FluentValidation;

namespace Application.Features.Organizations.Employees.AssignWeeklyShifts;

public class AssignWeeklyShiftsCommandValidator : AbstractValidator<AssignWeeklyShiftsCommand>
{
    public AssignWeeklyShiftsCommandValidator()
    {
        RuleFor(c => c.Id).NotEmpty();

        RuleFor(c => c.Days)
            .NotNull()
            .Must(days => days.Select(d => d.DayOfWeek).Distinct().Count() == days.Count)
            .WithMessage("Each day of the week may appear at most once.");

        RuleForEach(c => c.Days).ChildRules(day =>
        {
            day.RuleFor(d => d.DayOfWeek).InclusiveBetween(0, 6);
            day.RuleFor(d => d.ShiftId).NotEmpty();
        });
    }
}
