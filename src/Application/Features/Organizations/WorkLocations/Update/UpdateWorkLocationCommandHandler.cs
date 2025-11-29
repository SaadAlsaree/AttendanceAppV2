using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Organizations;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Organizations.WorkLocations.Update;

internal sealed class UpdateWorkLocationCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<UpdateWorkLocationCommand>
{
    public async Task<Result> Handle(UpdateWorkLocationCommand command, CancellationToken cancellationToken)
    {
        WorkLocation? workLocation = await context.WorkLocations
            .SingleOrDefaultAsync(wl => wl.Id == command.Id, cancellationToken);

        if (workLocation is null)
        {
            return Result.Failure(WorkLocationErrors.NotFound(command.Id));
        }

        // Check if work location name already exists for this organization (excluding current work location)
        WorkLocation? existingWorkLocation = await context.WorkLocations.AsNoTracking()
            .SingleOrDefaultAsync(wl => wl.OrganizationId == workLocation.OrganizationId &&
                                       wl.Name == command.Name &&
                                       wl.Id != command.Id, cancellationToken);

        if (existingWorkLocation is not null)
        {
            return Result.Failure(WorkLocationErrors.DuplicateName(command.Name));
        }

        // Validate coordinates
        if (command.Latitude < -90 || command.Latitude > 90)
        {
            return Result.Failure(WorkLocationErrors.InvalidCoordinates(command.Latitude, command.Longitude));
        }

        if (command.Longitude < -180 || command.Longitude > 180)
        {
            return Result.Failure(WorkLocationErrors.InvalidCoordinates(command.Latitude, command.Longitude));
        }

        // Validate radius
        if (command.RadiusMeters <= 0 || command.RadiusMeters > 10000)
        {
            return Result.Failure(WorkLocationErrors.InvalidRadius(command.RadiusMeters));
        }

        workLocation.Name = command.Name;
        workLocation.Address = command.Address;
        workLocation.Latitude = command.Latitude;
        workLocation.Longitude = command.Longitude;
        workLocation.RadiusMeters = command.RadiusMeters;
        workLocation.IsActive = command.IsActive;
        workLocation.Description = command.Description;
        workLocation.WifiSSID = command.WifiSSID;
        workLocation.BeaconId = command.BeaconId;
        workLocation.LastUpdatedAt = dateTimeProvider.GetUtcNow();

        workLocation.Raise(new WorkLocationUpdatedDomainEvent(workLocation.Id, workLocation.Name));

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
