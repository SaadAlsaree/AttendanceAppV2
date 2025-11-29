using Domain.Enums;
using FluentValidation;

namespace Application.Features.Users.UpdateRole;

public sealed class UpdateUserRoleCommandValidator : AbstractValidator<UpdateUserRoleCommand>
{
    public UpdateUserRoleCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.NewRole)
            .NotEmpty().WithMessage("New role is required")
            .WithMessage("Invalid role specified");


        RuleFor(x => x.UpdatedBy)
            .NotEmpty().WithMessage("Administrator ID is required");
    }
}
