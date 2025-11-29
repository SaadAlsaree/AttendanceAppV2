using FluentValidation;

namespace Application.Attendance.AttendanceBreaks.Delete;

public class DeleteAttendanceBreakCommandValidator : AbstractValidator<DeleteAttendanceBreakCommand>
{
    public DeleteAttendanceBreakCommandValidator()
    {
        RuleFor(c => c.AttendanceBreakId)
            .NotEmpty()
            .WithMessage("Attendance break ID is required");
    }
}
