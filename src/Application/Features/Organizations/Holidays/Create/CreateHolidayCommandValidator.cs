using FluentValidation;

namespace Application.Organizations.Holidays.Create;

public class CreateHolidayCommandValidator : AbstractValidator<CreateHolidayCommand>
{
    public CreateHolidayCommandValidator()
    {
        RuleFor(c => c.OrganizationId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty().MaximumLength(255);
        RuleFor(c => c.Date).NotEmpty().GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today));
    }
}
