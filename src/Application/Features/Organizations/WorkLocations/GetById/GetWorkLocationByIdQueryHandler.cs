using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Organizations;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Organizations.WorkLocations.GetById;

internal sealed class GetWorkLocationByIdQueryHandler(
    IApplicationDbContext context)
    : IQueryHandler<GetWorkLocationByIdQuery, ApiResponse<WorkLocationResponse>>
{
    public async Task<Result<ApiResponse<WorkLocationResponse>>> Handle(GetWorkLocationByIdQuery query, CancellationToken cancellationToken)
    {
        WorkLocation? workLocation = await context.WorkLocations
            .AsNoTracking()
            .SingleOrDefaultAsync(wl => wl.Id == query.WorkLocationId, cancellationToken);

        if (workLocation is null)
        {
            return Result.Failure<ApiResponse<WorkLocationResponse>>(WorkLocationErrors.NotFound(query.WorkLocationId));
        }

        var response = new WorkLocationResponse
        {
            Id = workLocation.Id,
            OrganizationId = workLocation.OrganizationId,
            Name = workLocation.Name,
            Address = workLocation.Address,
            Latitude = workLocation.Latitude,
            Longitude = workLocation.Longitude,
            RadiusMeters = workLocation.RadiusMeters,
            IsActive = workLocation.IsActive,
            Description = workLocation.Description,
            WifiSSID = workLocation.WifiSSID,
            BeaconId = workLocation.BeaconId,
            CreatedAt = workLocation.CreatedAt,
            UpdatedAt = workLocation.LastUpdatedAt
        };

        return ApiResponse<WorkLocationResponse>.Success(response);
    }
}
