using FluentValidation;

namespace Application.Features.Organizations.Sites.SetUnits;

public class SetSiteUnitsCommandValidator : AbstractValidator<SetSiteUnitsCommand>
{
    public SetSiteUnitsCommandValidator()
    {
        RuleFor(c => c.SiteId)
            .NotEmpty()
            .WithMessage("Site id is required");

        RuleFor(c => c.OrganizationalUnitIds)
            .NotNull()
            .WithMessage("Organizational unit ids are required (send an empty list to clear the site)");

        RuleForEach(c => c.OrganizationalUnitIds)
            .NotEmpty()
            .WithMessage("Organizational unit id cannot be empty");
    }
}
