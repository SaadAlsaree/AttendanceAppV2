using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Devices;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Devices.GetById;

internal sealed class GetDeviceByIdQueryHandler(
    IApplicationDbContext context)
    : IQueryHandler<GetDeviceByIdQuery, DeviceResponse>
{
    public async Task<Result<DeviceResponse>> Handle(GetDeviceByIdQuery query, CancellationToken cancellationToken)
    {
        Device? device = await context.Devices
            .Include(d => d.Organization)
            .AsNoTracking()
            .SingleOrDefaultAsync(d => d.Id == query.DeviceId, cancellationToken);

        if (device is null)
        {
            return Result.Failure<DeviceResponse>(DeviceErrors.NotFound(query.DeviceId));
        }

        var response = new DeviceResponse
        {
            Id = device.Id,
            Username = device.Username,
            Password = device.Password,
            Location = device.Location,
            IpAddress = device.IpAddress,
            DeviceId = device.DeviceId,
            IsupKey = device.IsupKey,
            Port = device.Port,
            Protocol = device.Protocol,
            DeviceModel = device.DeviceModel,
            SerialNumber = device.SerialNumber,
            MacAddress = device.MacAddress,
            FirmwareVersion = device.FirmwareVersion,
            Department = device.Department,
            Features = device.Features,
            IsActive = device.IsActive,
            LastConnected = device.LastConnected,
            OrganizationId = device.OrganizationId,
            CreatedAt = device.CreatedAt,
            UpdatedAt = device.LastUpdatedAt ?? DateTime.UtcNow,
            OrganizationName = device.Organization?.UnitName,

        };

        return response;
    }
}
