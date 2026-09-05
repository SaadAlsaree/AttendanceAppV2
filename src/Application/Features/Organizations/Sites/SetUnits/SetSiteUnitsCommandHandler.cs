using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Organizations;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Organizations.Sites.SetUnits;

internal sealed class SetSiteUnitsCommandHandler(
    IApplicationDbContext context,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<SetSiteUnitsCommand>
{
    public async Task<Result> Handle(SetSiteUnitsCommand command, CancellationToken cancellationToken)
    {
        bool siteExists = await context.Sites
            .AsNoTracking()
            .AnyAsync(s => s.Id == command.SiteId && !s.IsDeleted, cancellationToken);

        if (!siteExists)
        {
            return Result.Failure(SiteErrors.NotFound(command.SiteId));
        }

        var requestedUnitIds = command.OrganizationalUnitIds.Distinct().ToList();

        // Load the units that are currently in the site plus the ones being added, in one query.
        // Note this deliberately does NOT expand ChildUnits: assigning a unit says nothing about
        // its descendants, which is the whole point of a site as opposed to a unit subtree.
        List<OrganizationalUnit> affectedUnits = await context.OrganizationalUnits
            .Where(ou => !ou.IsDeleted &&
                         (ou.SiteId == command.SiteId || requestedUnitIds.Contains(ou.Id)))
            .ToListAsync(cancellationToken);

        var foundIds = affectedUnits.Select(ou => ou.Id).ToHashSet();
        var missingUnitIds = requestedUnitIds.Where(id => !foundIds.Contains(id)).ToList();

        if (missingUnitIds.Count > 0)
        {
            return Result.Failure(OrganizationErrors.OrganizationalUnit.NotFound(missingUnitIds[0]));
        }

        DateTime now = dateTimeProvider.GetUtcNow();
        Guid updatedBy = userContext.UserId;
        var requestedSet = requestedUnitIds.ToHashSet();

        foreach (OrganizationalUnit unit in affectedUnits)
        {
            // Assigning a unit that belongs to another site moves it here — a unit has at most one
            // site, so this is the intended way to transfer one.
            Guid? newSiteId = requestedSet.Contains(unit.Id) ? command.SiteId : null;

            if (unit.SiteId == newSiteId)
            {
                continue;
            }

            unit.SiteId = newSiteId;
            unit.LastUpdatedAt = now;
            unit.LastUpdatedBy = updatedBy;
        }

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
