using FluentValidation;

namespace Application.Organizations.OrganizationalUnits.Update;

public class UpdateOrganizationalUnitCommandValidator : AbstractValidator<UpdateOrganizationalUnitCommand>
{
    public UpdateOrganizationalUnitCommandValidator()
    {
        RuleFor(c => c.OrganizationalUnitId)
            .NotEmpty()
            .WithMessage("Organizational unit ID is required");

        RuleFor(c => c.UnitName)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("Unit name is required and cannot exceed 100 characters");

        RuleFor(c => c.UnitCode)
            .NotEmpty()
            .MaximumLength(20)
            .WithMessage("Unit code is required, must be uppercase alphanumeric, and cannot exceed 20 characters");


        RuleFor(c => c.UnitLevel)
            .GreaterThanOrEqualTo(1)
            .When(c => c.UnitLevel.HasValue)
            .WithMessage("Unit level must be greater than or equal to 1");
    }
}
