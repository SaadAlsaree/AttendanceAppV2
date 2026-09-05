using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Models;
using Domain.Entities.Organizations;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Organizations.Sites.GetById;

internal sealed class GetSiteByIdQueryHandler(
    IApplicationDbContext context,
    IUserContext userContext)
    : IQueryHandler<GetSiteByIdQuery, SiteDetailsResponse>
{
    public async Task<Result<SiteDetailsResponse>> Handle(GetSiteByIdQuery query, CancellationToken cancellationToken)
    {
        UserInfoDto user = await userContext.GetUserAsync();

        if (user.Role == Role.SiteSupervisor && user.SiteId != query.Id)
        {
            return Result.Failure<SiteDetailsResponse>(SiteErrors.AccessDenied);
        }

        SiteDetailsResponse? site = await context.Sites
            .AsNoTracking()
            .Where(s => s.Id == query.Id && !s.IsDeleted)
            .Select(s => new SiteDetailsResponse
            {
                Id = s.Id,
                SiteName = s.SiteName,
                SiteCode = s.SiteCode,
                Description = s.Description,
                Address = s.Address,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.LastUpdatedAt,
                OrganizationalUnits = s.OrganizationalUnits
                    .Where(ou => !ou.IsDeleted)
                    .OrderBy(ou => ou.UnitName)
                    .Select(ou => new SiteUnitResponse
                    {
                        Id = ou.Id,
                        UnitName = ou.UnitName,
                        UnitCode = ou.UnitCode,
                        ParentUnitName = ou.ParentUnit != null ? ou.ParentUnit.UnitName : null,
                        EmployeeCount = ou.Employees.Count
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (site is null)
        {
            return Result.Failure<SiteDetailsResponse>(SiteErrors.NotFound(query.Id));
        }

        return site;
    }
}
