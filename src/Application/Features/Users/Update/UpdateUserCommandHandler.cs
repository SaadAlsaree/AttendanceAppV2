using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Organizations;
using Domain.Entities.Users;
using Domain.Enums;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Users.Update;

internal sealed class UpdateUserCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext)
    : ICommandHandler<UpdateUserCommand>
{
    public async Task<Result> Handle(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        User? user = await context.Users
            .FirstOrDefaultAsync(u => u.Id == command.Id, cancellationToken);

        if (user is null)
        {
            return Result.Failure(UserErrors.NotFound(command.Id));
        }

        // Check if UserLogin is being changed and if it's unique
        if (user.UserLogin != command.UserLogin)
        {
            bool userLoginExists = await context.Users
                .AsNoTracking()
                .AnyAsync(u => u.UserLogin == command.UserLogin && u.Id != command.Id, cancellationToken);

            if (userLoginExists)
            {
                return Result.Failure(UserErrors.UserLoginAlreadyExists(command.UserLogin));
            }
        }

        // Check if organizational unit exists (if provided)
        if (command.OrganizationalUnitId.HasValue)
        {
            OrganizationalUnit? organizationalUnit = await context.OrganizationalUnits
                .SingleOrDefaultAsync(o => o.Id == command.OrganizationalUnitId.Value, cancellationToken);

            if (organizationalUnit is null)
            {
                return Result.Failure(OrganizationErrors.OrganizationalUnit.NotFound(command.OrganizationalUnitId.Value));
            }
        }

        // Check if site exists (if provided)
        if (command.SiteId.HasValue)
        {
            bool siteExists = await context.Sites
                .AsNoTracking()
                .AnyAsync(s => s.Id == command.SiteId.Value && !s.IsDeleted, cancellationToken);

            if (!siteExists)
            {
                return Result.Failure(SiteErrors.NotFound(command.SiteId.Value));
            }
        }

        // Verify the role is a valid enum value
        if (!Enum.IsDefined<Role>(command.Role))
        {
            return Result.Failure(UserErrors.InvalidRole());
        }

        // A SiteSupervisor's entire access scope comes from their site. Without one they would be
        // able to sign in but see nothing, so refuse to create that state rather than letting it
        // look like a data bug later.
        if (command.Role == Role.SiteSupervisor && !command.SiteId.HasValue)
        {
            return Result.Failure(SiteErrors.NoSiteAssigned);
        }

        // Verify the status is a valid enum value
        if (!Enum.IsDefined<UserStatus>(command.Status))
        {
            return Result.Failure(UserErrors.InvalidStatus());
        }

        // Update user properties
        // Always assign values to ensure EF Core detects changes
        user.Username = command.Username;
        user.UserLogin = command.UserLogin;
        user.Role = command.Role;
        user.Status = command.Status;
        user.IsActive = command.IsActive;
        user.OrganizationalUnitId = command.OrganizationalUnitId;
        user.SiteId = command.SiteId;
        user.LastUpdatedAt = dateTimeProvider.GetUtcNow();
        user.LastUpdatedBy = userContext.UserId;

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

