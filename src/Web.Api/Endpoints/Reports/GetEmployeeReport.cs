using Application.Abstractions.Messaging;
using Application.Features.Reports.GetEmployeeReport;
using Infrastructure.Authentication;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Reports;

internal sealed class GetEmployeeReport : IEndpoint
{
    public sealed class Request
    {
        public Guid EmployeeId { get; set; }
        public DateOnly FromDate { get; set; }
        public DateOnly ToDate { get; set; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("reports/employee", async (
            [AsParameters] Request request,
            IQueryHandler<GetEmployeeReportQuery, ApiResponse<GetEmployeeReportVm>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetEmployeeReportQuery
            {
                EmployeeId = request.EmployeeId,
                FromDate = request.FromDate,
                ToDate = request.ToDate
            };

            Result<ApiResponse<GetEmployeeReportVm>> result = await handler.Handle(query, cancellationToken);

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

                // تقرير الموظف (من - إلى) متاح للأدمن فقط
                string[] allowedRoles = ["Admin", "SuperAdmin"];
                return allowedRoles.Contains(userRole, StringComparer.OrdinalIgnoreCase);
            }))
        .WithName("GetEmployeeReport")
        .WithSummary("تقرير موظف")
        .WithDescription("تقرير حضور وانصراف موظف واحد ضمن مدى زمني (من - إلى) — متاح للأدمن فقط")
        .WithOpenApi()
        .RequireRateLimiting("per-user");
    }
}
