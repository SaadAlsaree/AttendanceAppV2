using FluentValidation;

namespace Application.Attendance.AttendanceLogs.Update;

public class UpdateAttendanceLogCommandValidator : AbstractValidator<UpdateAttendanceLogCommand>
{
    public UpdateAttendanceLogCommandValidator()
    {
        RuleFor(c => c.AttendanceLogId).NotEmpty();
        RuleFor(c => c.CardNo).MaximumLength(50).When(x => !string.IsNullOrEmpty(x.CardNo));
        RuleFor(c => c.EmpID).MaximumLength(50).When(x => !string.IsNullOrEmpty(x.EmpID));
        RuleFor(c => c.DateWork).NotEmpty();
        RuleFor(c => c.TimeAttend).NotEmpty();
        RuleFor(c => c.Direct).NotEmpty();
        RuleFor(c => c.DeviceName).MaximumLength(100).When(x => !string.IsNullOrEmpty(x.DeviceName));
        RuleFor(c => c.DeviceNo).MaximumLength(50).When(x => !string.IsNullOrEmpty(x.DeviceNo));
    }
}
