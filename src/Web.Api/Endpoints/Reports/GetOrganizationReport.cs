using Application.Abstractions.Messaging;
using Application.Features.Reports.GetOrganizationReport;
using Infrastructure.Authentication;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Reports;

internal sealed class GetOrganizationReport : IEndpoint
{
    public sealed class Request
    {
        public Guid OrganizationalUnitId { get; set; }
        public DateOnly? Date { get; set; }
        public Guid? ShiftId { get; set; }
        public bool IncludeSubUnits { get; set; } = true;
        public string? SearchTerm { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("reports/organization", async (
            [AsParameters] Request request,
            IQueryHandler<GetOrganizationReportQuery, ApiResponse<GetOrganizationReportVm>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetOrganizationReportQuery
            {
                OrganizationalUnitId = request.OrganizationalUnitId,
                Date = request.Date,
                ShiftId = request.ShiftId,
                IncludeSubUnits = request.IncludeSubUnits,
                SearchTerm = request.SearchTerm,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };

            Result<ApiResponse<GetOrganizationReportVm>> result = await handler.Handle(query, cancellationToken);

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
         string[] allowedRoles = ["Admin", "Employee", "Manager", "SuperAdmin", "OrgSupervisor", "SiteSupervisor"];
         return allowedRoles.Contains(userRole, StringComparer.OrdinalIgnoreCase);
     }))
        .WithName("GetOrganizationReport")
        .WithSummary("تقرير المؤسسة")
        .WithDescription("تقرير شامل للمؤسسة يتضمن إحصائيات الحضور والانصراف والموظفين مع تفصيل الوحدات التنظيمية")
        .WithOpenApi()
        .RequireRateLimiting("per-user");
    }
}
