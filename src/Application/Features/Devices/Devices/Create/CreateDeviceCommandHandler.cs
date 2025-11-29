using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Devices;
using Domain.Entities.Organizations;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Devices.Create;

internal sealed class CreateDeviceCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<CreateDeviceCommand, bool>
{
    public async Task<Result<bool>> Handle(CreateDeviceCommand command, CancellationToken cancellationToken)
    {
        // Check if device with same serial number already exists
        bool deviceIdExists = await context.Devices
            .AsNoTracking()
            .AnyAsync(d => d.SerialNumber == command.SerialNumber, cancellationToken);

        if (deviceIdExists)
        {
            return Result.Failure<bool>(DeviceErrors.DuplicateSerialNumber(command.SerialNumber ?? string.Empty));
        }



        // Validate IP address format
        if (!System.Net.IPAddress.TryParse(command.IpAddress, out _))
        {
            return Result.Failure<bool>(DeviceErrors.InvalidIpAddress(command.IpAddress));
        }

        // Check if IP address is already in use
        bool deviceWithIpExists = await context.Devices
            .AsNoTracking()
            .AnyAsync(d => d.IpAddress == command.IpAddress, cancellationToken);

        if (deviceWithIpExists)
        {
            return Result.Failure<bool>(DeviceErrors.DuplicateSerialNumber($"IP address {command.IpAddress}"));
        }

        // Validate organization exists if provided
        if (command.OrganizationId.HasValue)
        {
            OrganizationalUnit? organization = await context.OrganizationalUnits
                .AsNoTracking()
                .SingleOrDefaultAsync(o => o.Id == command.OrganizationId, cancellationToken);

            if (organization is null)
            {
                return Result.Failure<bool>(OrganizationErrors.OrganizationalUnit.NotFound(command.OrganizationId.Value));
            }
        }



        var device = new Device
        {
            Username = command.Username,
            Password = command.Password,
            Location = command.Location,
            IpAddress = command.IpAddress,
            DeviceId = command.DeviceId,
            IsupKey = command.IsupKey,
            Port = command.Port,
            Protocol = command.Protocol,
            DeviceModel = command.DeviceModel,
            SerialNumber = command.SerialNumber,
            MacAddress = command.MacAddress,
            FirmwareVersion = command.FirmwareVersion,
            Department = command.Department,
            Features = command.Features,
            IsActive = command.IsActive,
            LastConnected = command.LastConnected,
            OrganizationId = command.OrganizationId,
            CreatedAt = dateTimeProvider.GetUtcNow()
        };

        device.Raise(new DeviceCreatedDomainEvent(device.Id, device.Username ?? string.Empty, device.Password ?? string.Empty, device.IpAddress ?? string.Empty));

        context.Devices.Add(device);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}
