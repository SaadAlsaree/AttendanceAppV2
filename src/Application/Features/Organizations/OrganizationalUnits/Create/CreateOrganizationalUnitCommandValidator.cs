using FluentValidation;

namespace Application.Organizations.OrganizationalUnits.Create;

public class CreateOrganizationalUnitCommandValidator : AbstractValidator<CreateOrganizationalUnitCommand>
{
    public CreateOrganizationalUnitCommandValidator()
    {
        RuleFor(c => c.UnitName)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("Unit name is required and cannot exceed 100 characters");

        RuleFor(c => c.UnitCode)
            .NotEmpty()
            .MaximumLength(20)
            .WithMessage("Unit code is required, and cannot exceed 2 characters");

        RuleFor(c => c.UnitDescription)
            .MaximumLength(500)
            .When(c => !string.IsNullOrEmpty(c.UnitDescription))
            .WithMessage("Unit description cannot exceed 500 characters");

        RuleFor(c => c.Email)
            .EmailAddress()
            .When(c => !string.IsNullOrEmpty(c.Email))
            .WithMessage("Email must be a valid email address");

        RuleFor(c => c.PhoneNumber)
            .MaximumLength(20)
            .When(c => !string.IsNullOrEmpty(c.PhoneNumber))
            .WithMessage("Phone number cannot exceed 20 characters");

        RuleFor(c => c.Address)
            .MaximumLength(200)
            .When(c => !string.IsNullOrEmpty(c.Address))
            .WithMessage("Address cannot exceed 200 characters");

        RuleFor(c => c.PostalCode)
            .MaximumLength(10)
            .When(c => !string.IsNullOrEmpty(c.PostalCode))
            .WithMessage("Postal code cannot exceed 10 characters");

        RuleFor(c => c.UnitLevel)
            .GreaterThanOrEqualTo(1)
            .When(c => c.UnitLevel.HasValue)
            .WithMessage("Unit level must be greater than or equal to 1");
    }
}
