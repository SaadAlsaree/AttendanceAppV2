using Application.Abstractions.Messaging;
using Application.Attendance.Reports.GetOvertimeReport;
using Domain.Enums;
using Infrastructure.Authentication;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Reports;

internal sealed class GetOvertimeReport : IEndpoint
{
    public sealed class Request
    {
        // Nullable so a missing required param yields an explicit 400 (not a 500 from model binding).
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public Guid? OrganizationId { get; set; }
        public Guid? OrganizationalUnitId { get; set; }
        public Guid? EmployeeId { get; set; }
        public ReportType? ReportType { get; set; }
        public int? MinOvertimeHours { get; set; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("reports/overtime", async (
            [AsParameters] Request request,
            IQueryHandler<GetOvertimeReportQuery, OvertimeReportResponse> handler,
            CancellationToken cancellationToken) =>
        {
            // فلترة من - إلى مطلوبة لاحتساب العمل الإضافي للأشهر السابقة
            if (request.StartDate is null || request.EndDate is null)
            {
                return Results.BadRequest(new
                {
                    error = "StartDate and EndDate are required",
                    messageAr = "يجب تحديد تاريخ البداية وتاريخ النهاية (من - إلى)"
                });
            }

            var query = new GetOvertimeReportQuery
            {
                // Normalize to full days (inclusive end-of-day) and force Kind=Utc — the Attendance
                // `date` column is `timestamptz`, and Npgsql rejects Unspecified-kind DateTimes.
                StartDate = DateTime.SpecifyKind(request.StartDate.Value.Date, DateTimeKind.Utc),
                EndDate = DateTime.SpecifyKind(
                    request.EndDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc),
                OrganizationId = request.OrganizationId,
                OrganizationalUnitId = request.OrganizationalUnitId,
                EmployeeId = request.EmployeeId,
                ReportType = request.ReportType ?? ReportType.Custom,
                MinOvertimeHours = request.MinOvertimeHours
            };

            Result<OvertimeReportResponse> result = await handler.Handle(query, cancellationToken);

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

                // تقرير العمل الإضافي متاح للأدمن والمدير (عارضي التقارير)
                string[] allowedRoles = ["Admin", "SuperAdmin", "Manager"];
                return allowedRoles.Contains(userRole, StringComparer.OrdinalIgnoreCase);
            }))
        .WithName("GetOvertimeReport")
        .WithSummary("تقرير العمل الإضافي")
        .WithDescription("احتساب ساعات العمل الإضافي مع فلترة من - إلى تشمل الأشهر السابقة، مجمّعة لكل موظف وجهة — متاح للأدمن والمدير")
        .WithOpenApi()
        .RequireRateLimiting("per-user");
    }
}
