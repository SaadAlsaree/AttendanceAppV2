using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Organizations;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Organizations.Sites.Create;

internal sealed class CreateSiteCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<CreateSiteCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateSiteCommand command, CancellationToken cancellationToken)
    {
        bool siteCodeExists = await context.Sites
            .AsNoTracking()
            .AnyAsync(s => s.SiteCode == command.SiteCode && !s.IsDeleted, cancellationToken);

        if (siteCodeExists)
        {
            return Result.Failure<Guid>(SiteErrors.SiteCodeAlreadyExists);
        }

        var site = new Site
        {
            SiteName = command.SiteName,
            SiteCode = command.SiteCode,
            Description = command.Description,
            Address = command.Address,
            IsActive = command.IsActive,
            CreatedAt = dateTimeProvider.GetUtcNow()
        };

        context.Sites.Add(site);

        await context.SaveChangesAsync(cancellationToken);

        return site.Id;
    }
}
