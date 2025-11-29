using FluentValidation;

namespace Application.Attendance.AttendanceSchedules.Update;

public class UpdateAttendanceScheduleCommandValidator : AbstractValidator<UpdateAttendanceScheduleCommand>
{
    public UpdateAttendanceScheduleCommandValidator()
    {
        RuleFor(c => c.AttendanceScheduleId)
            .NotEmpty()
            .WithMessage("Attendance schedule ID is required");

        RuleFor(c => c.StartDate)
            .LessThan(c => c.EndDate)
            .When(c => c.StartDate.HasValue && c.EndDate.HasValue)
            .WithMessage("Start date must be before end date");

        RuleFor(c => c.Notes)
            .MaximumLength(500)
            .When(c => !string.IsNullOrEmpty(c.Notes))
            .WithMessage("Notes cannot exceed 500 characters");
    }
}
