using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Organizations;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Organizations.Sites.Update;

internal sealed class UpdateSiteCommandHandler(
    IApplicationDbContext context,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<UpdateSiteCommand>
{
    public async Task<Result> Handle(UpdateSiteCommand command, CancellationToken cancellationToken)
    {
        Site? site = await context.Sites
            .FirstOrDefaultAsync(s => s.Id == command.Id && !s.IsDeleted, cancellationToken);

        if (site is null)
        {
            return Result.Failure(SiteErrors.NotFound(command.Id));
        }

        bool siteCodeTaken = await context.Sites
            .AsNoTracking()
            .AnyAsync(s => s.SiteCode == command.SiteCode && s.Id != command.Id && !s.IsDeleted, cancellationToken);

        if (siteCodeTaken)
        {
            return Result.Failure(SiteErrors.SiteCodeAlreadyExists);
        }

        // Unit membership is NOT edited here — it has its own command so that a form which does not
        // know about units cannot silently clear them.
        site.SiteName = command.SiteName;
        site.SiteCode = command.SiteCode;
        site.Description = command.Description;
        site.Address = command.Address;
        site.IsActive = command.IsActive;
        site.LastUpdatedAt = dateTimeProvider.GetUtcNow();
        site.LastUpdatedBy = userContext.UserId;

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
