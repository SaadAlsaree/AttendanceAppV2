using FluentValidation;
using Domain.Enums;

namespace Application.Attendance.AttendanceBreaks.Update;

public class UpdateAttendanceBreakCommandValidator : AbstractValidator<UpdateAttendanceBreakCommand>
{
    public UpdateAttendanceBreakCommandValidator()
    {
        RuleFor(c => c.AttendanceBreakId)
            .NotEmpty()
            .WithMessage("Attendance break ID is required");

        RuleFor(c => c.BreakType)
            .IsInEnum()
            .When(c => c.BreakType.HasValue)
            .WithMessage("Break type must be a valid value");

        RuleFor(c => c.DurationMinutes)
            .GreaterThan(0)
            .LessThanOrEqualTo(480)
            .When(c => c.DurationMinutes.HasValue)
            .WithMessage("Duration must be between 1 and 480 minutes");

        RuleFor(c => c.EndTime)
            .GreaterThan(c => c.StartTime)
            .When(c => c.EndTime.HasValue && c.StartTime.HasValue)
            .WithMessage("End time must be after start time");

        RuleFor(c => c.Notes)
            .MaximumLength(500)
            .When(c => !string.IsNullOrEmpty(c.Notes))
            .WithMessage("Notes cannot exceed 500 characters");
    }
}
