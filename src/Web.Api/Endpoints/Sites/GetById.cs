using Application.Abstractions.Messaging;
using Application.Features.Organizations.Sites.GetById;
using Infrastructure.Authentication;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Sites;

internal sealed class GetById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("sites/{id:guid}", async (
            Guid id,
            IQueryHandler<GetSiteByIdQuery, SiteDetailsResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetSiteByIdQuery(id);

            Result<SiteDetailsResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Sites)
        .RequireAuthorization(policy => policy
     .RequireAssertion(context =>
     {
         if (context.User.Identity?.IsAuthenticated != true)
         {
             return false;
         }

         string? userRole = context.User.GetRole();

         if (string.IsNullOrWhiteSpace(userRole))
         {
             return false;
         }

         string[] allowedRoles = ["Admin", "SuperAdmin", "Manager", "SiteSupervisor"];
         return allowedRoles.Contains(userRole, StringComparer.OrdinalIgnoreCase);
     }))
     .RequireRateLimiting("per-user");
    }
}
