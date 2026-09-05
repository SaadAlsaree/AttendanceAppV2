using Application.Abstractions.Messaging;
using Application.Features.Organizations.Sites.Get;
using Infrastructure.Authentication;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Sites;

internal sealed class Get : IEndpoint
{
    public sealed class Request
    {
        public string? SearchText { get; set; }
        public bool? IsActive { get; set; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("sites", async (
            [AsParameters] Request request,
            IQueryHandler<GetSitesQuery, List<SiteResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetSitesQuery(request.SearchText, request.IsActive);

            Result<List<SiteResponse>> result = await handler.Handle(query, cancellationToken);

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

         // SiteSupervisor is included so the UI can name the site it supervises; the handler
         // restricts the result to that one site.
         string[] allowedRoles = ["Admin", "SuperAdmin", "Manager", "SiteSupervisor"];
         return allowedRoles.Contains(userRole, StringComparer.OrdinalIgnoreCase);
     }))
     .RequireRateLimiting("per-user");
    }
}
