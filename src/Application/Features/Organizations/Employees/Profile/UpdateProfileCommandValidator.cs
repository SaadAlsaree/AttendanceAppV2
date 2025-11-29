using FluentValidation;

namespace Application.Features.Organizations.Employees.Profile;

public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ProfileImage)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.ProfileImage));
    }
}
