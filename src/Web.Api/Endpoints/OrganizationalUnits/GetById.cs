using Application.Abstractions.Messaging;
using Application.Organizations.OrganizationalUnits.GetById;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.OrganizationalUnits;

internal sealed class GetById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("organizational-units/{id:guid}", async (
            Guid id,
            IQueryHandler<GetOrganizationalUnitByIdQuery, OrganizationalUnitResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetOrganizationalUnitByIdQuery(id);
            Result<OrganizationalUnitResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.OrganizationalUnits)
        .RequireAuthorization();
    }
}
