using FluentValidation;

namespace Application.Organizations.OrganizationalUnits.Delete;

public class DeleteOrganizationalUnitCommandValidator : AbstractValidator<DeleteOrganizationalUnitCommand>
{
    public DeleteOrganizationalUnitCommandValidator()
    {
        RuleFor(c => c.OrganizationalUnitId)
            .NotEmpty()
            .WithMessage("Organizational unit ID is required");
    }
}
