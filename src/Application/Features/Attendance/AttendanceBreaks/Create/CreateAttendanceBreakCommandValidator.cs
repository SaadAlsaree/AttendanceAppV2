using FluentValidation;

namespace Application.Attendance.AttendanceBreaks.Create;

public class CreateAttendanceBreakCommandValidator : AbstractValidator<CreateAttendanceBreakCommand>
{
    public CreateAttendanceBreakCommandValidator()
    {
        RuleFor(c => c.AttendanceId)
            .NotEmpty()
            .WithMessage("Attendance ID is required");

        RuleFor(c => c.StartTime)
            .NotEmpty()
            .WithMessage("Start time is required");

        RuleFor(c => c.BreakType)
            .IsInEnum()
            .WithMessage("Break type must be a valid value");

        RuleFor(c => c.DurationMinutes)
            .GreaterThan(0)
            .LessThanOrEqualTo(480)
            .When(c => c.DurationMinutes.HasValue)
            .WithMessage("Duration must be between 1 and 480 minutes");

        RuleFor(c => c.EndTime)
            .GreaterThan(c => c.StartTime)
            .When(c => c.EndTime.HasValue)
            .WithMessage("End time must be after start time");

        RuleFor(c => c.Notes)
            .MaximumLength(500)
            .When(c => !string.IsNullOrEmpty(c.Notes))
            .WithMessage("Notes cannot exceed 500 characters");

        // Either EndTime or DurationMinutes must be provided
        RuleFor(c => c)
            .Must(c => c.EndTime.HasValue || c.DurationMinutes.HasValue)
            .WithMessage("Either end time or duration must be provided");
    }
}
