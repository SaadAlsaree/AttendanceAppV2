using Application.Abstractions.Messaging;
using Application.Features.Dashboard.GetDashboardStats;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Dashboard;

internal sealed class GetDashboardStats : IEndpoint
{
    public sealed class Request
    {
        public Guid OrganizationId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public Guid? DepartmentId { get; set; }
        public Guid? EmployeeId { get; set; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("dashboard/stats", async (
            [AsParameters] Request request,
            IQueryHandler<GetDashboardStatsQuery, DashboardStatsResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetDashboardStatsQuery(
                OrganizationId: request.OrganizationId,
                StartDate: request.StartDate,
                EndDate: request.EndDate,
                DepartmentId: request.DepartmentId,
                EmployeeId: request.EmployeeId
            );

            Result<DashboardStatsResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Dashboard)
        .RequireAuthorization();
    }
}
