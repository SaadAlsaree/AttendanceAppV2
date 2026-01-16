using Application.Abstractions.Messaging;
using Application.Features.Reports.GetOrganizationalSummary;
using Infrastructure.Authentication;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Reports;

internal sealed class GetOrganizationalSummary : IEndpoint
{
    public sealed class Request
    {
        public Guid? OrganizationalUnitId { get; set; }
        public bool IncludeSubUnits { get; set; } = true;
        public DateTime? Date { get; set; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("reports/organizational-summary", async (
            [AsParameters] Request request,
            IQueryHandler<GetOrganizationalSummaryQuery, ApiResponse<OrganizationalSummaryVm>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetOrganizationalSummaryQuery
            {
                OrganizationalUnitId = request.OrganizationalUnitId,
                IncludeSubUnits = request.IncludeSubUnits,
                Date = request.Date
            };

            Result<ApiResponse<OrganizationalSummaryVm>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Reports)
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
        .WithName("GetOrganizationalSummary")
        .WithSummary("الملخص التنظيمي")
        .WithDescription("ملخص مختصر للوحدات التنظيمية يتضمن عدد الموظفين والشفتات ومعدلات الحضور")
        .WithOpenApi()
        .RequireRateLimiting("per-user");
    }
}
