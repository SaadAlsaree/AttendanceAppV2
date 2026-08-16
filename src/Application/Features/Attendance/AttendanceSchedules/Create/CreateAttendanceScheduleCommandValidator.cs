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

        // Every schedule day must fall inside [StartDate, EndDate]
        RuleFor(c => c.ScheduleDays)
            .Must((command, days) => days.All(d =>
                d.ScheduleDayDate >= command.StartDate &&
                (!command.EndDate.HasValue || d.ScheduleDayDate <= command.EndDate.Value)))
            .WithMessage("All schedule day dates must fall within the schedule's start and end dates")
            .When(c => c.ScheduleDays != null && c.ScheduleDays.Any());

        // Schedule day dates must be unique
        RuleFor(c => c.ScheduleDays)
            .Must(days => days.Select(d => d.ScheduleDayDate).Distinct().Count() == days.Count)
            .WithMessage("Schedule day dates must be unique")
            .When(c => c.ScheduleDays != null && c.ScheduleDays.Any());

        // Active schedule days must reference a shift
        RuleFor(c => c.ScheduleDays)
            .Must(days => days.All(d => !d.IsActive || d.ShiftId != Guid.Empty))
            .WithMessage("Active schedule days must have a shift assigned")
            .When(c => c.ScheduleDays != null && c.ScheduleDays.Any());

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
                .When(c => c.IsActive)
                .WithMessage("Shift ID is required for active schedule days");

            RuleFor(c => c.Notes)
                .MaximumLength(500)
                .When(c => !string.IsNullOrEmpty(c.Notes))
                .WithMessage("Notes cannot exceed 500 characters");
        }
    }
}
