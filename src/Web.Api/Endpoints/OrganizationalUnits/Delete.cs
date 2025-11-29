using Application.Abstractions.Messaging;
using Application.Organizations.OrganizationalUnits.Delete;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.OrganizationalUnits;

internal sealed class Delete : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("organizational-units/{id:guid}", async (
            Guid id,
            ICommandHandler<DeleteOrganizationalUnitCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new DeleteOrganizationalUnitCommand(id);
            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.OrganizationalUnits)
        .RequireAuthorization();
    }
}
