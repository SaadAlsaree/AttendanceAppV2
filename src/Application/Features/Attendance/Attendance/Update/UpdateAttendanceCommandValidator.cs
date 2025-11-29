using FluentValidation;

namespace Application.Attendance.Update;

public sealed class UpdateAttendanceCommandValidator : AbstractValidator<UpdateAttendanceCommand>
{
    public UpdateAttendanceCommandValidator()
    {
        RuleFor(x => x.AttendanceId).NotEmpty();
        // Removed location-related validation rules
    }
}
