using System;
using FluentValidation;

namespace Application.Features.Attendance.AttendanceSchedules.BulkInsert;

public class BulkInsertAttendanceScheduleCommandValidator : AbstractValidator<BulkInsertAttendanceScheduleCommand>
{
    public BulkInsertAttendanceScheduleCommandValidator()
    {
        RuleFor(x => x.StartDate)
            .NotEmpty()
            .WithMessage("Start date is required.");
        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .WithMessage("End date must be greater than start date.");
        RuleFor(x => x.ShiftId)
            .NotEmpty()
            .WithMessage("Shift ID is required.");
    }
}
