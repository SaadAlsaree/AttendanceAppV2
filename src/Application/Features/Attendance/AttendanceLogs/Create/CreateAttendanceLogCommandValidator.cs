using FluentValidation;

namespace Application.Attendance.AttendanceLogs.Create;

public class CreateAttendanceLogCommandValidator : AbstractValidator<CreateAttendanceLogCommand>
{
    public CreateAttendanceLogCommandValidator()
    {
        RuleFor(c => c.EmpID).NotEmpty();
        RuleFor(c => c.DateTimeAttend).NotEmpty().LessThanOrEqualTo(DateTime.Now);
        RuleFor(c => c.DateWork).NotEmpty();
        RuleFor(c => c.TimeAttend).NotEmpty();
        RuleFor(c => c.Direct).NotEmpty();
        RuleFor(c => c.DeviceName).NotEmpty().MaximumLength(100);
        RuleFor(c => c.DeviceNo).NotEmpty().MaximumLength(50);
        RuleFor(c => c.CardNo).NotEmpty().MaximumLength(50);
    }
}
