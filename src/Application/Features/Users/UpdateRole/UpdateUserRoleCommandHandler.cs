using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Attendance;
using Domain.Entities.Users;
using Domain.Enums;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Users.UpdateRole;

internal sealed class UpdateUserRoleCommandHandler(
    IApplicationDbContext context)
    : ICommandHandler<UpdateUserRoleCommand, ApiResponse<bool>>
{
    public async Task<Result<ApiResponse<bool>>> Handle(UpdateUserRoleCommand command, CancellationToken cancellationToken)
    {
        // Find the user by ID
        User? user = await context.Users
            .FirstOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<ApiResponse<bool>>(UserErrors.NotFound(command.UserId));
        }

        // Verify user is active
        if (!user.IsActive)
        {
            return Result.Failure<ApiResponse<bool>>(UserErrors.AccountInactive());
        }

        // Verify the role is a valid enum value
        if (!Enum.IsDefined(command.NewRole))
        {
            return Result.Failure<ApiResponse<bool>>(UserErrors.InvalidRole());
        }

        // Update user's role
        user.Role = command.NewRole;
        user.LastUpdatedAt = DateTime.Now;
        user.LastUpdatedBy = command.UpdatedBy;

        // Log role change for security audit
        var securityAuditLog = new SecurityAuditLog
        {
            UserId = user.Id,
            EventType = "RoleChange",
            EventDescription = $"Role changed to {command.NewRole}",
            Timestamp = DateTime.Now,
            Severity = "Info",
            IsSuccessful = true,
            AdditionalData = $"{{\"UpdatedBy\": \"{command.UpdatedBy}\", \"PreviousRole\": \"{user.Role}\"}}"
        };

        context.SecurityAuditLogs.Add(securityAuditLog);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(new ApiResponse<bool>
        {
            Data = true,
            Message = $"User role updated to {command.NewRole} successfully"
        });
    }
}
