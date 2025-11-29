using Application.Abstractions.Messaging;
using Application.Devices.Delete;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Devices;

internal sealed class Delete : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("devices/{id:guid}", async (
            Guid id,
            ICommandHandler<DeleteDeviceCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new DeleteDeviceCommand
            {
                DeviceId = id
            };

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Devices)
        .RequireAuthorization();
    }
}




