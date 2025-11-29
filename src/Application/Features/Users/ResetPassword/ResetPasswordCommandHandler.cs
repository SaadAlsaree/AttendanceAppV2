using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Users;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Users.ResetPassword;

internal sealed class ResetPasswordCommandHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher)
    : ICommandHandler<ResetPasswordCommand, bool>
{
    public async Task<Result<bool>> Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        // Find user by login
        User? user = await context.Users
            .FirstOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<bool>(UserErrors.NotFound(command.UserId));
        }

        // Check if user account is active
        if (!user.IsActive || user.Status != Domain.Enums.UserStatus.Active)
        {
            return Result.Failure<bool>(UserErrors.AccountInactive());
        }

        // Hash the new password
        string passwordHash = passwordHasher.Hash(command.NewPassword);

        // Update user's password
        user.PasswordHash = passwordHash;
        user.LastUpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
