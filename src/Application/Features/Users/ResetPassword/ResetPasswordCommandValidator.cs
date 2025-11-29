using FluentValidation;
using Application.Features.Users.ResetPassword;

namespace Application.Features.Users.ResetPassword;

internal sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithMessage("New password is required")
            .MinimumLength(6)
            .WithMessage("Password must be at least 8 characters long")
            ;

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty()
            .WithMessage("Password confirmation is required")
            .Equal(x => x.NewPassword)
            .WithMessage("Password confirmation must match the new password");
    }
}
