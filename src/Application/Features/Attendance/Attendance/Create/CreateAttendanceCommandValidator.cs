using FluentValidation;

namespace Application.Attendance.Create;

public class CreateAttendanceCommandValidator : AbstractValidator<CreateAttendanceCommand>
{
    public CreateAttendanceCommandValidator()
    {
        RuleFor(c => c.EmployeeId).NotEmpty().WithMessage("Employee ID is required");
        RuleFor(c => c.OrganizationId).NotEmpty().WithMessage("Organization ID is required");
        RuleFor(c => c.Date).NotEmpty().WithMessage("Date is required");
        RuleFor(c => c.Date).LessThanOrEqualTo(DateTime.Today).WithMessage("Date cannot be in the future");
        RuleFor(c => c.Notes).MaximumLength(500).When(c => !string.IsNullOrEmpty(c.Notes)).WithMessage("Notes cannot exceed 500 characters");
    }
}
