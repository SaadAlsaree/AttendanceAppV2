using Application.Abstractions.Messaging;
using Application.Features.Organizations.Sites.SetUnits;
using Infrastructure.Authentication;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Sites;

/// <summary>
/// Replaces a site's unit membership wholesale. Only the listed units join the site — their child
/// units do not follow, which is the behaviour that distinguishes a site from a unit subtree.
/// </summary>
internal sealed class SetUnits : IEndpoint
{
    public sealed class Request
    {
        public List<Guid> OrganizationalUnitIds { get; set; } = new();
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("sites/{id:guid}/units", async (
            Guid id,
            Request request,
            ICommandHandler<SetSiteUnitsCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new SetSiteUnitsCommand(id, request.OrganizationalUnitIds);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
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

         // Write endpoint — SiteSupervisor must never appear here: it would let the role widen its
         // own access scope by adding units to its site.
         string[] allowedRoles = ["Admin", "SuperAdmin"];
         return allowedRoles.Contains(userRole, StringComparer.OrdinalIgnoreCase);
     }))
     .RequireRateLimiting("per-user");
    }
}
