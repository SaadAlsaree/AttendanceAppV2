using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Organizations;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Organizations.Sites.Delete;

internal sealed class DeleteSiteCommandHandler(
    IApplicationDbContext context,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<DeleteSiteCommand>
{
    public async Task<Result> Handle(DeleteSiteCommand command, CancellationToken cancellationToken)
    {
        Site? site = await context.Sites
            .Include(s => s.OrganizationalUnits)
            .Include(s => s.Users)
            .FirstOrDefaultAsync(s => s.Id == command.Id && !s.IsDeleted, cancellationToken);

        if (site is null)
        {
            return Result.Failure(SiteErrors.NotFound(command.Id));
        }

        // Refuse while anything still points at the site. Deleting it out from under a supervisor
        // would silently reduce their scope to nothing rather than telling anyone.
        if (site.OrganizationalUnits.Count > 0)
        {
            return Result.Failure(SiteErrors.HasAssignedUnits);
        }

        if (site.Users.Count > 0)
        {
            return Result.Failure(SiteErrors.HasAssignedUsers);
        }

        // Soft delete: every read path filters on !IsDeleted, and the guards above guarantee no
        // unit or user is left pointing at a deleted row.
        site.IsDeleted = true;
        site.DeletedAt = dateTimeProvider.GetUtcNow();
        site.DeletedBy = userContext.UserId;

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
