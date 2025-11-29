using FluentValidation;

namespace Application.Features.Users.SignUp;

public sealed class SignUpCommandValidator : AbstractValidator<SignUpCommand>
{
    public SignUpCommandValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required")
            .MaximumLength(100).WithMessage("Username cannot exceed 100 characters");

        RuleFor(x => x.UserLogin)
            .NotEmpty().WithMessage("User login is required")
            .MaximumLength(100).WithMessage("User login cannot exceed 100 characters")
            .Matches("^[a-zA-Z0-9_]+$").WithMessage("User login can only contain letters, numbers, and underscores");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters long");
           


        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Password confirmation is required")
            .Equal(x => x.Password).WithMessage("Passwords do not match");

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Invalid role specified");
    }
}
