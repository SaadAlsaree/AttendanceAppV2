using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Devices;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Devices.Update;

internal sealed class UpdateDeviceCommandHandler : ICommandHandler<UpdateDeviceCommand, bool>

{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateDeviceCommandHandler(IApplicationDbContext context, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<bool>> Handle(UpdateDeviceCommand command, CancellationToken cancellationToken)
    {
        Device? device = await _context.Devices.FirstOrDefaultAsync(d => d.Id == command.Id, cancellationToken);
        if (device == null)
        {
            return Result.Failure<bool>(DeviceErrors.NotFound(command.Id));
        }

        device.Username = command.Username;
        device.Password = command.Password;
        device.DeviceId = command.DeviceId;
        device.IsupKey = command.IsupKey;
        device.Port = command.Port;
        device.Protocol = command.Protocol;
        device.DeviceModel = command.DeviceModel;
        device.SerialNumber = command.SerialNumber;
        device.MacAddress = command.MacAddress;
        device.FirmwareVersion = command.FirmwareVersion;
        device.Department = command.Department;
        device.Features = command.Features;
        device.IsActive = command.IsActive;
        device.LastConnected = command.LastConnected;
        device.OrganizationId = command.OrganizationId;
        device.LastUpdatedAt = _dateTimeProvider.GetUtcNow();
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success(true);
    }
}
