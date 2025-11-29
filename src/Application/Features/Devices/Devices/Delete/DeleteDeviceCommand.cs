using Application.Abstractions.Messaging;

namespace Application.Devices.Delete;

public sealed class DeleteDeviceCommand : ICommand
{
    public Guid DeviceId { get; set; }
}
