using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Attendance;
using Domain.Entities.Users;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Users.ChangePassword;

internal sealed class ChangePasswordCommandHandler(
    IApplicationDbContext context,
 IDateTimeProvider dateTimeProvider,
    IPasswordHasher passwordHasher)
    : ICommandHandler<ChangePasswordCommand, ApiResponse<bool>>
{
    public async Task<Result<ApiResponse<bool>>> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        // Find the user by ID
        User user = await context.Users
            .FirstOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<ApiResponse<bool>>(UserErrors.NotFound(command.UserId));
        }

        // Verify user is active
        if (!user.IsActive)
        {
            return Result.Failure<ApiResponse<bool>>(UserErrors.Unauthorized());
        }

        // Verify current password
        if (!passwordHasher.Verify(command.CurrentPassword, user.PasswordHash))
        {
            return Result.Failure<ApiResponse<bool>>(UserErrors.InvalidCurrentPassword(command.CurrentPassword));
        }

        // Verify new password is not the same as current
        if (command.CurrentPassword == command.NewPassword)
        {
            return Result.Failure<ApiResponse<bool>>(UserErrors.NewPasswordSameAsCurrent());
        }

        // Update password hash
        user.PasswordHash = passwordHasher.Hash(command.NewPassword);
        user.LastUpdatedAt = dateTimeProvider.GetUtcNow();

        // Log password change for security audit
        var securityAuditLog = new SecurityAuditLog
        {
            UserId = user.Id,
            EventType = "PasswordChange",
            EventDescription = "Password changed successfully",
            Timestamp = dateTimeProvider.GetUtcNow(),
            IpAddress = "", // This would be populated from the request context in a real implementation
            UserAgent = ""  // This would be populated from the request context in a real implementation
        };

        context.SecurityAuditLogs.Add(securityAuditLog);
        context.Users.Update(user);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(new ApiResponse<bool>
        {
            Data = true,
            Message = "Password changed successfully"
        });
    }
}
