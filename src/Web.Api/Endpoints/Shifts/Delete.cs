using Application.Abstractions.Messaging;
using Application.Features.Organizations.Shifts.Delete;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Shifts;

internal sealed class Delete : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("shifts/{id:guid}", async (
            Guid id,
            ICommandHandler<DeleteShiftCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new DeleteShiftCommand { ShiftId = id };
            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Shifts)
        .RequireAuthorization();
    }
}
