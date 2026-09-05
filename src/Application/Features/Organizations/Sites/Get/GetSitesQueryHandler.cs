using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Models;
using Domain.Entities.Organizations;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Organizations.Sites.Get;

internal sealed class GetSitesQueryHandler(
    IApplicationDbContext context,
    IUserContext userContext)
    : IQueryHandler<GetSitesQuery, List<SiteResponse>>
{
    public async Task<Result<List<SiteResponse>>> Handle(GetSitesQuery query, CancellationToken cancellationToken)
    {
        UserInfoDto user = await userContext.GetUserAsync();

        IQueryable<Site> sitesQuery = context.Sites
            .AsNoTracking()
            .Where(s => !s.IsDeleted);

        // A SiteSupervisor may only ever see their own site — the endpoint is granted to them so the
        // UI can name the site they supervise, not so they can browse the others.
        if (user.Role == Role.SiteSupervisor)
        {
            if (user.SiteId is not { } siteId)
            {
                return new List<SiteResponse>();
            }

            sitesQuery = sitesQuery.Where(s => s.Id == siteId);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchText))
        {
            string searchText = query.SearchText.Trim();
            sitesQuery = sitesQuery.Where(s =>
                s.SiteName.Contains(searchText) ||
                s.SiteCode.Contains(searchText));
        }

        if (query.IsActive.HasValue)
        {
            sitesQuery = sitesQuery.Where(s => s.IsActive == query.IsActive.Value);
        }

        List<SiteResponse> sites = await sitesQuery
            .OrderBy(s => s.SiteName)
            .Select(s => new SiteResponse
            {
                Id = s.Id,
                SiteName = s.SiteName,
                SiteCode = s.SiteCode,
                Description = s.Description,
                Address = s.Address,
                IsActive = s.IsActive,
                UnitCount = s.OrganizationalUnits.Count(ou => !ou.IsDeleted),
                UserCount = s.Users.Count(u => !u.IsDeleted),
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.LastUpdatedAt
            })
            .ToListAsync(cancellationToken);

        return sites;
    }
}
