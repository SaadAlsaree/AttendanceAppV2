using Application.Abstractions.Messaging;
using Domain.Enums;
using SharedKernel;

namespace Application.Devices.Get;

public sealed class GetDevicesQuery : IQuery<PaginatedResponse<DeviceResponse>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SearchTerm { get; set; }
}
