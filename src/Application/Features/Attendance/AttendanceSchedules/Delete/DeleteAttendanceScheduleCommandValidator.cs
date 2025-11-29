using FluentValidation;

namespace Application.Attendance.AttendanceSchedules.Delete;

public class DeleteAttendanceScheduleCommandValidator : AbstractValidator<DeleteAttendanceScheduleCommand>
{
    public DeleteAttendanceScheduleCommandValidator()
    {
        RuleFor(c => c.AttendanceScheduleId)
            .NotEmpty()
            .WithMessage("Attendance schedule ID is required");
    }
}
