using Application.Abstractions.Messaging;
using Application.Features.Reports.GetOrganizationReport;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Reports;

internal sealed class GetOrganizationReport : IEndpoint
{
    public sealed class Request
    {
        public Guid OrganizationalUnitId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public Guid? ShiftId { get; set; }
        public bool IncludeSubUnits { get; set; } = true;
        public string? SearchTerm { get; set; }
        public int Page { get; set; } = 1;
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
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                ShiftId = request.ShiftId,
                IncludeSubUnits = request.IncludeSubUnits,
                SearchTerm = request.SearchTerm,
                PageNumber = request.Page,
                PageSize = request.PageSize
            };

            Result<ApiResponse<GetOrganizationReportVm>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Reports)
        .RequireAuthorization()
        .WithName("GetOrganizationReport")
        .WithSummary("تقرير المؤسسة")
        .WithDescription("تقرير شامل للمؤسسة يتضمن إحصائيات الحضور والانصراف والموظفين مع تفصيل الوحدات التنظيمية")
        .WithOpenApi();
    }
}
