using FluentValidation;

namespace Application.Features.Organizations.Sites.Update;

public class UpdateSiteCommandValidator : AbstractValidator<UpdateSiteCommand>
{
    public UpdateSiteCommandValidator()
    {
        RuleFor(c => c.Id)
            .NotEmpty()
            .WithMessage("Site id is required");

        RuleFor(c => c.SiteName)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("Site name is required and cannot exceed 100 characters");

        RuleFor(c => c.SiteCode)
            .NotEmpty()
            .MaximumLength(20)
            .WithMessage("Site code is required and cannot exceed 20 characters");

        RuleFor(c => c.Description)
            .MaximumLength(500)
            .When(c => !string.IsNullOrEmpty(c.Description))
            .WithMessage("Description cannot exceed 500 characters");

        RuleFor(c => c.Address)
            .MaximumLength(500)
            .When(c => !string.IsNullOrEmpty(c.Address))
            .WithMessage("Address cannot exceed 500 characters");
    }
}
