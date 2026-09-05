using Application.Abstractions.Messaging;
using Application.Features.Organizations.Sites.Update;
using Infrastructure.Authentication;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Sites;

internal sealed class Update : IEndpoint
{
    public sealed class Request
    {
        public string SiteName { get; set; } = string.Empty;
        public string SiteCode { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Address { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("sites/{id:guid}", async (
            Guid id,
            Request request,
            ICommandHandler<UpdateSiteCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateSiteCommand
            {
                Id = id,
                SiteName = request.SiteName,
                SiteCode = request.SiteCode,
                Description = request.Description,
                Address = request.Address,
                IsActive = request.IsActive
            };

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

         // Write endpoint — SiteSupervisor is view-only and must never appear here.
         string[] allowedRoles = ["Admin", "SuperAdmin"];
         return allowedRoles.Contains(userRole, StringComparer.OrdinalIgnoreCase);
     }))
     .RequireRateLimiting("per-user");
    }
}
