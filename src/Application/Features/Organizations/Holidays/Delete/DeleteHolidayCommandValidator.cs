using FluentValidation;

namespace Application.Organizations.Holidays.Delete;

public class DeleteHolidayCommandValidator : AbstractValidator<DeleteHolidayCommand>
{
    public DeleteHolidayCommandValidator()
    {
        RuleFor(c => c.HolidayId).NotEmpty();
    }
}
