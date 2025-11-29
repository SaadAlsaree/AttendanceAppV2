using Application.Abstractions.Messaging;

namespace Application.Devices.GetById;

public sealed class GetDeviceByIdQuery : IQuery<DeviceResponse>
{
    public Guid DeviceId { get; set; }
}
