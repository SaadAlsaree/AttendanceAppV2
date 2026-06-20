using Application.Abstractions.Messaging;
using Application.Organizations.OrganizationalUnits.GetAsTree;
using Infrastructure.Authentication;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.OrganizationalUnits;

internal sealed class GetAsTree : IEndpoint
{
    public sealed class Request
    {
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("organizational-units/tree", async (
            [AsParameters] Request request,
            IQueryHandler<GetOrganizationalUnitsAsTreeQuery, List<OrganizationalUnitTreeResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetOrganizationalUnitsAsTreeQuery();

            Result<List<OrganizationalUnitTreeResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.OrganizationalUnits)
        .WithName("GetOrganizationalUnitsAsTree")
        .WithSummary("Get organizational units as hierarchical tree")
        .WithDescription("Retrieves all organizational units in a hierarchical tree structure with aggregated counts")
        .Produces<List<OrganizationalUnitTreeResponse>>(200, "application/json")
        .ProducesProblem(400)
        .ProducesProblem(500)
         .RequireAuthorization(policy => policy
     .RequireAssertion(context =>
     {
         if (context.User.Identity?.IsAuthenticated != true)
         {
             return false;
         }

         // الحصول على Role من JWT Token Claims
         string? userRole = context.User.GetRole();

         if (string.IsNullOrWhiteSpace(userRole))
         {
             return false;
         }

         // OR logic: إذا كان لديه أي Role من الأدوار المطلوبة
         string[] allowedRoles = ["Admin", "Employee", "Manager", "SuperAdmin", "SecurityOfficer"];
         return allowedRoles.Contains(userRole, StringComparer.OrdinalIgnoreCase);
     }))
     .RequireRateLimiting("per-user");
    }
}
