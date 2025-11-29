using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Organizations;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Organizations.WorkLocations.Create;

internal sealed class CreateWorkLocationCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<CreateWorkLocationCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateWorkLocationCommand command, CancellationToken cancellationToken)
    {
        // Check if organizational unit exists
        OrganizationalUnit? organizationalUnit = await context.OrganizationalUnits.AsNoTracking()
            .SingleOrDefaultAsync(ou => ou.Id == command.OrganizationId, cancellationToken);

        if (organizationalUnit is null)
        {
            return Result.Failure<Guid>(OrganizationErrors.OrganizationalUnit.NotFound(command.OrganizationId));
        }

        // Check if work location name already exists for this organization
        WorkLocation? existingWorkLocation = await context.WorkLocations.AsNoTracking()
            .SingleOrDefaultAsync(wl => wl.OrganizationId == command.OrganizationId &&
                                       wl.Name == command.Name, cancellationToken);

        if (existingWorkLocation is not null)
        {
            return Result.Failure<Guid>(WorkLocationErrors.DuplicateName(command.Name));
        }

        // Validate coordinates
        if (command.Latitude < -90 || command.Latitude > 90)
        {
            return Result.Failure<Guid>(WorkLocationErrors.InvalidCoordinates(command.Latitude, command.Longitude));
        }

        if (command.Longitude < -180 || command.Longitude > 180)
        {
            return Result.Failure<Guid>(WorkLocationErrors.InvalidCoordinates(command.Latitude, command.Longitude));
        }

        // Validate radius
        if (command.RadiusMeters <= 0 || command.RadiusMeters > 10000)
        {
            return Result.Failure<Guid>(WorkLocationErrors.InvalidRadius(command.RadiusMeters));
        }

        var workLocation = new WorkLocation
        {
            OrganizationId = command.OrganizationId,
            Name = command.Name,
            Address = command.Address,
            Latitude = command.Latitude,
            Longitude = command.Longitude,
            RadiusMeters = command.RadiusMeters,
            IsActive = command.IsActive,
            Description = command.Description,
            WifiSSID = command.WifiSSID,
            BeaconId = command.BeaconId,
            CreatedAt = dateTimeProvider.GetUtcNow()
        };

        workLocation.Raise(new WorkLocationCreatedDomainEvent(workLocation.Id, workLocation.OrganizationId, workLocation.Name));

        context.WorkLocations.Add(workLocation);

        await context.SaveChangesAsync(cancellationToken);

        return workLocation.Id;
    }
}
