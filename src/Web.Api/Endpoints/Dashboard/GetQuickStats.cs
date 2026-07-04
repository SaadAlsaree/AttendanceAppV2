using Application.Abstractions.Messaging;
using Application.Features.Dashboard.GetQuickStats;
using Infrastructure.Authentication;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Dashboard;

internal sealed class GetQuickStats : IEndpoint
{
    public sealed class Request
    {
        public Guid OrganizationId { get; set; }
        public DateTime? Date { get; set; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("dashboard/quick-stats", async (
            [AsParameters] Request request,
            IQueryHandler<GetQuickStatsQuery, ApiResponse<QuickStatsResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetQuickStatsQuery(
                OrganizationId: request.OrganizationId,
                Date: request.Date
            );

            Result<ApiResponse<QuickStatsResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Dashboard)
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
         string[] allowedRoles = ["Admin", "SuperAdmin", "OrgSupervisor"];
         return allowedRoles.Contains(userRole, StringComparer.OrdinalIgnoreCase);
     }))
     .RequireRateLimiting("per-user");
    }
}
