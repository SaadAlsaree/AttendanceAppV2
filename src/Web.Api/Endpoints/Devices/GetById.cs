using Application.Abstractions.Messaging;
using Application.Devices.GetById;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Devices;

internal sealed class GetById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("devices/{id:guid}", async (
            Guid id,
            IQueryHandler<GetDeviceByIdQuery, DeviceResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetDeviceByIdQuery
            {
                DeviceId = id
            };

            Result<DeviceResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Devices)
        .RequireAuthorization();
    }
}




