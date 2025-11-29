using FluentValidation;

namespace Application.Features.Users.Update;

public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(c => c.Id).NotEmpty();

        RuleFor(c => c.Username)
            .NotEmpty().WithMessage("Username is required")
            .MaximumLength(100).WithMessage("Username cannot exceed 100 characters");

        RuleFor(c => c.UserLogin)
            .NotEmpty().WithMessage("User login is required")
            .MaximumLength(50).WithMessage("User login cannot exceed 50 characters")
            .Matches("^[a-zA-Z0-9_]+$").WithMessage("User login can only contain letters, numbers, and underscores");

        RuleFor(c => c.Role)
            .IsInEnum().WithMessage("Invalid role specified");

        RuleFor(c => c.Status)
            .IsInEnum().WithMessage("Invalid status specified");
    }
}

