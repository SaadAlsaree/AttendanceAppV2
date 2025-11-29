using FluentValidation;

namespace Application.Features.Attendance.AttendanceSchedules.UpdateScheduleDays;

public sealed class UpdateScheduleDaysCommandValidator : AbstractValidator<UpdateScheduleDaysCommand>
{
    public UpdateScheduleDaysCommandValidator()
    {
        RuleFor(c => c.AttendanceScheduleId)
            .NotEmpty()
            .WithMessage("Attendance schedule ID is required");

        RuleFor(c => c.ScheduleDays)
            .NotEmpty()
            .WithMessage("At least one schedule day must be provided");

        RuleForEach(c => c.ScheduleDays)
            .SetValidator(new UpdateScheduleDayCommandValidator());
    }
}

public sealed class UpdateScheduleDayCommandValidator : AbstractValidator<UpdateScheduleDayCommand>
{
    public UpdateScheduleDayCommandValidator()
    {
        RuleFor(c => c.Id)
            .NotEmpty()
            .WithMessage("Schedule day ID is required");

        RuleFor(c => c.ShiftId)
            .NotEmpty()
            .WithMessage("Shift ID is required");

        RuleFor(c => c.Notes)
            .MaximumLength(500)
            .When(c => !string.IsNullOrEmpty(c.Notes))
            .WithMessage("Notes cannot exceed 500 characters");
    }
}
