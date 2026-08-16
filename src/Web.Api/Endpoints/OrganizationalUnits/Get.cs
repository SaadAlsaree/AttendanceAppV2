using Application.Abstractions.Messaging;
using Application.Organizations.OrganizationalUnits.Get;
using Infrastructure.Authentication;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.OrganizationalUnits;

internal sealed class Get : IEndpoint
{
    public sealed class Request
    {
        public string? SearchText { get; set; }
        public Guid? ParentUnitId { get; set; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("organizational-units", async (
            [AsParameters] Request request,
            IQueryHandler<GetOrganizationalUnitsQuery, List<OrganizationalUnitResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetOrganizationalUnitsQuery(
                request.SearchText,
                request.ParentUnitId);

            Result<List<OrganizationalUnitResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.OrganizationalUnits)
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
         string[] allowedRoles = ["Admin", "Employee", "Manager", "SuperAdmin"];
         return allowedRoles.Contains(userRole, StringComparer.OrdinalIgnoreCase);
     }))
     .RequireRateLimiting("per-user");
    }
}
