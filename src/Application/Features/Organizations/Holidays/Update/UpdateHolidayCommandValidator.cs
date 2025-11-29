using FluentValidation;

namespace Application.Organizations.Holidays.Update;

public class UpdateHolidayCommandValidator : AbstractValidator<UpdateHolidayCommand>
{
    public UpdateHolidayCommandValidator()
    {
        RuleFor(c => c.HolidayId).NotEmpty();
        RuleFor(c => c.Name).MaximumLength(255).When(x => !string.IsNullOrEmpty(x.Name));
        RuleFor(c => c.Date).GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today)).When(x => x.Date.HasValue);
    }
}
