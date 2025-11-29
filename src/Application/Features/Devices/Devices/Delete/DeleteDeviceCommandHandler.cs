using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Devices;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Devices.Delete;

internal sealed class DeleteDeviceCommandHandler(
    IApplicationDbContext context)
    : ICommandHandler<DeleteDeviceCommand>
{
    public async Task<Result> Handle(DeleteDeviceCommand command, CancellationToken cancellationToken)
    {
        Device? device = await context.Devices
            .SingleOrDefaultAsync(d => d.Id == command.DeviceId, cancellationToken);

        if (device is null)
        {
            return Result.Failure(DeviceErrors.NotFound(command.DeviceId));
        }





        context.Devices.Remove(device);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
