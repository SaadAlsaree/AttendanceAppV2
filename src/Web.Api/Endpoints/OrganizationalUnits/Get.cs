using Application.Abstractions.Messaging;
using Application.Organizations.OrganizationalUnits.Get;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.OrganizationalUnits;

internal sealed class Get : IEndpoint
{
    public sealed class Request
    {
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("organizational-units", async (
            [AsParameters] Request request,
            IQueryHandler<GetOrganizationalUnitsQuery, List<OrganizationalUnitResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetOrganizationalUnitsQuery();

            Result<List<OrganizationalUnitResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.OrganizationalUnits)
        .RequireAuthorization();
    }
}
