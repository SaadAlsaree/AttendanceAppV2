using Application.Abstractions.Messaging;
using Application.Devices.Get;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Devices;

internal sealed class Get : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("devices", async (
            [AsParameters] GetDevicesQuery query,
            IQueryHandler<GetDevicesQuery, PaginatedResponse<DeviceResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            Result<PaginatedResponse<DeviceResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Devices)
        .RequireAuthorization();
    }
}




