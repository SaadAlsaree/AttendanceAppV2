using FluentValidation;

namespace Application.Attendance.CheckIn;

internal sealed class CheckInCommandValidator : AbstractValidator<CheckInCommand>
{
    public CheckInCommandValidator()
    {
        RuleFor(x => x.EmployeeId)
            .NotEmpty()
            .WithMessage("Employee ID is required.");

        RuleFor(x => x.CheckInMethod)
            .NotEmpty()
            .WithMessage("Check-in time is required.");

        RuleFor(x => x.CheckOutMethod)
            .NotEmpty()
            .WithMessage("Check-out method is required.");

        RuleFor(x => x.CardNo)
            .NotEmpty()
            .MaximumLength(50)
            .WithMessage("Card number is required and cannot exceed 50 characters.");

        RuleFor(x => x.EmpID)
.NotEmpty()
.MaximumLength(50)
.WithMessage("Employee ID is required and cannot exceed 50 characters.");

        RuleFor(x => x.DateWork)
            .NotEmpty()
            .WithMessage("Date work is required.");

        RuleFor(x => x.TimeAttend)
            .NotEmpty()
            .WithMessage("Time attend is required.");

        RuleFor(x => x.Direct)
            .NotEmpty()
            .WithMessage("Direct is required.");

        RuleFor(x => x.DeviceName)
            .MaximumLength(100)
            .When(x => x.DeviceName is not null)
                    .WithMessage("Device name cannot exceed 100 characters.");

        RuleFor(x => x.DeviceNo)
            .MaximumLength(500)
            .When(x => x.DeviceNo is not null)
            .WithMessage("Device no cannot exceed 500 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000)
            .When(x => x.Notes is not null)
            .WithMessage("Notes cannot exceed 1000 characters.");
    }
}
