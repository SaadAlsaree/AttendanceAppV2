using Application.Abstractions.Messaging;
using Application.Features.Dashboard.GetQuickStats;
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
        .RequireAuthorization();
    }
}
