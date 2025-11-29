using FluentValidation;

namespace Application.Features.Users.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.UserLogin)
            .NotEmpty().WithMessage("User login is required")
            .MaximumLength(100).WithMessage("User login cannot exceed 100 characters");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters long");
    }
}
