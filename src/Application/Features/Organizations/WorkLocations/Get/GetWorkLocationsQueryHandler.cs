using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Organizations;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Organizations.WorkLocations.Get;

internal sealed class GetWorkLocationsQueryHandler(
    IApplicationDbContext context)
    : IQueryHandler<GetWorkLocationsQuery, PaginatedResponse<WorkLocationResponse>>
{
    public async Task<Result<PaginatedResponse<WorkLocationResponse>>> Handle(GetWorkLocationsQuery query, CancellationToken cancellationToken)
    {
        // Check if organizational unit exists
        OrganizationalUnit? organizationalUnit = await context.OrganizationalUnits.AsNoTracking()
            .SingleOrDefaultAsync(ou => ou.Id == query.OrganizationId, cancellationToken);

        if (organizationalUnit is null)
        {
            return Result.Failure<PaginatedResponse<WorkLocationResponse>>(OrganizationErrors.OrganizationalUnit.NotFound(query.OrganizationId));
        }

        List<WorkLocationResponse> workLocations = await context.WorkLocations
            .AsNoTracking()
            .Where(wl => wl.OrganizationId == query.OrganizationId)
            .OrderBy(wl => wl.Name)
            .Select(wl => new WorkLocationResponse
            {
                Id = wl.Id,
                OrganizationId = wl.OrganizationId,
                Name = wl.Name,
                Address = wl.Address,
                Latitude = wl.Latitude,
                Longitude = wl.Longitude,
                RadiusMeters = wl.RadiusMeters,
                IsActive = wl.IsActive,
                Description = wl.Description,
                WifiSSID = wl.WifiSSID,
                BeaconId = wl.BeaconId,
                CreatedAt = wl.CreatedAt,
                UpdatedAt = wl.LastUpdatedAt
            })
            .ToListAsync(cancellationToken);

        return PaginatedResponse<WorkLocationResponse>.Create(workLocations, workLocations.Count, query.PageNumber, query.PageSize);
    }
}
