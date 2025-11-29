using FluentValidation;

namespace Application.Attendance.AttendanceLogs.Delete;

public class DeleteAttendanceLogCommandValidator : AbstractValidator<DeleteAttendanceLogCommand>
{
    public DeleteAttendanceLogCommandValidator()
    {
        RuleFor(c => c.AttendanceLogId).NotEmpty();
    }
}
