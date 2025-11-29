using FluentValidation;

namespace Application.Attendance.AttendanceSchedules.Create;

public class CreateAttendanceScheduleCommandValidator : AbstractValidator<CreateAttendanceScheduleCommand>
{
    public CreateAttendanceScheduleCommandValidator()
    {
        RuleFor(c => c.EmployeeId)
            .NotEmpty()
            .WithMessage("Employee ID is required");

        RuleFor(c => c.StartDate)
            .NotEmpty()
            .WithMessage("Start date is required");

        RuleFor(c => c.ScheduleDays)
            .NotEmpty()
            .WithMessage("At least one schedule day is required");

        RuleForEach(c => c.ScheduleDays)
            .SetValidator(new CreateScheduleDayCommandValidator());


        RuleFor(c => c.ScheduleType)
        .IsInEnum()
        .WithMessage("Schedule type must be a valid value");

        RuleFor(c => c.Notes)
            .MaximumLength(500)
            .When(c => !string.IsNullOrEmpty(c.Notes))
            .WithMessage("Notes cannot exceed 500 characters");

        RuleFor(c => c.EndDate)
            .GreaterThan(c => c.StartDate)
            .When(c => c.EndDate.HasValue)
            .WithMessage("End date must be after start date");
    }

    private sealed class CreateScheduleDayCommandValidator : AbstractValidator<CreateScheduleDayCommand>
    {
        public CreateScheduleDayCommandValidator()
        {
            RuleFor(c => c.ScheduleDayDate)
                .NotEmpty()
                .WithMessage("Schedule day date is required");

            RuleFor(c => c.ShiftId)
                .NotEmpty()
                .WithMessage("Shift ID is required");

            RuleFor(c => c.IsActive)
                .NotEmpty()
                .WithMessage("Is active is required");

            RuleFor(c => c.Notes)
                .MaximumLength(500)
                .When(c => !string.IsNullOrEmpty(c.Notes))
                .WithMessage("Notes cannot exceed 500 characters");
        }
    }
}
