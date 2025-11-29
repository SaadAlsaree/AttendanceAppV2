using FluentValidation;

namespace Application.Attendance.CheckOut;

internal sealed class CheckOutCommandValidator : AbstractValidator<CheckOutCommand>
{
    public CheckOutCommandValidator()
    {
        RuleFor(x => x.EmployeeId)
            .NotEmpty()
            .WithMessage("Employee ID is required.");

        RuleFor(x => x.AttendanceId)
            .NotEmpty()
            .WithMessage("Attendance ID is required.");

        RuleFor(x => x.CheckOutTime)
            .NotEmpty()
            .WithMessage("Check-out time is required.");

        RuleFor(x => x.CardNo)
            .NotEmpty()
            .MaximumLength(50)
            .WithMessage("Card number is required and cannot exceed 50 characters.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("Name is required and cannot exceed 100 characters.");

        RuleFor(x => x.EmployeeNoString)
            .MaximumLength(50)
            .When(x => x.EmployeeNoString is not null)
            .WithMessage("Employee number string cannot exceed 50 characters.");

        RuleFor(x => x.UserType)
            .MaximumLength(50)
            .When(x => x.UserType is not null)
            .WithMessage("User type cannot exceed 50 characters.");

        RuleFor(x => x.CurrentVerifyMode)
            .MaximumLength(50)
            .When(x => x.CurrentVerifyMode is not null)
            .WithMessage("Current verify mode cannot exceed 50 characters.");

        RuleFor(x => x.AttendanceStatus)
            .MaximumLength(50)
            .When(x => x.AttendanceStatus is not null)
            .WithMessage("Attendance status cannot exceed 50 characters.");

        RuleFor(x => x.Label)
            .MaximumLength(100)
            .When(x => x.Label is not null)
            .WithMessage("Label cannot exceed 100 characters.");

        RuleFor(x => x.Mask)
            .MaximumLength(100)
            .When(x => x.Mask is not null)
            .WithMessage("Mask cannot exceed 100 characters.");

        RuleFor(x => x.PictureURL)
            .MaximumLength(500)
            .When(x => x.PictureURL is not null)
            .WithMessage("Picture URL cannot exceed 500 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000)
            .When(x => x.Notes is not null)
            .WithMessage("Notes cannot exceed 1000 characters.");
    }
}
