using Application.Abstractions.Messaging;
using Application.Features.Reports.GetAttendanceReport;
using Infrastructure.Authentication;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Reports;

internal sealed class GetAttendanceReport : IEndpoint
{
    public sealed class Request
    {
        public Guid OrganizationalUnitId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public Guid? ShiftId { get; set; }
        public bool IncludeSubUnits { get; set; } = true;
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("reports/attendance-summary", async (
            [AsParameters] Request request,
            IQueryHandler<GetAttendanceReportQuery, ApiResponse<AttendanceReportVm>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetAttendanceReportQuery
            {
                OrganizationalUnitId = request.OrganizationalUnitId,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                ShiftId = request.ShiftId,
                IncludeSubUnits = request.IncludeSubUnits
            };

            Result<ApiResponse<AttendanceReportVm>> result = await handler.Handle(query, cancellationToken);

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
        .WithName("GetAttendanceReport")
        .WithSummary("تقرير الحضور الشامل")
        .WithDescription("تقرير شامل لإحصائيات الحضور والانصراف مع تفصيل الشفتات والوحدات الفرعية والإجازات")
        .WithOpenApi()
        .RequireRateLimiting("per-user");
    }
}
