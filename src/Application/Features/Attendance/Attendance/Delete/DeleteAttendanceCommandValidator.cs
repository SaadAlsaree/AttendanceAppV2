using FluentValidation;

namespace Application.Attendance.Delete;

internal sealed class DeleteAttendanceCommandValidator : AbstractValidator<DeleteAttendanceCommand>
{
    public DeleteAttendanceCommandValidator()
    {
        RuleFor(x => x.AttendanceId)
            .NotEmpty()
            .WithMessage("Attendance ID is required.");
    }
}
