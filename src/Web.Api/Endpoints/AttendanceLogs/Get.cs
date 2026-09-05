using Application.Abstractions.Messaging;
using Application.Attendance.AttendanceLogs.Get;
using Infrastructure.Authentication;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.AttendanceLogs;

internal sealed class Get : IEndpoint
{
    public sealed class Request
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public Guid? OrganizationId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? Direct { get; set; }
        public string? DeviceName { get; set; }
        public string? DeviceNo { get; set; }
        public string? EmpID { get; set; }
        public string? EmpName { get; set; }
        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; }
        public string? SortOrder { get; set; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("attendance-logs", async (
            [AsParameters] Request request,
            IQueryHandler<GetAttendanceLogsQuery, PaginatedResponse<AttendanceLogResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetAttendanceLogsQuery(
                Page: request.Page,
                PageSize: request.PageSize,
                OrganizationId: request.OrganizationId,
                StartDate: request.StartDate,
                EndDate: request.EndDate,
                Direct: request.Direct,
                DeviceName: request.DeviceName,
                DeviceNo: request.DeviceNo,
                EmpID: request.EmpID,
                EmpName: request.EmpName,
                SearchTerm: request.SearchTerm,
                SortBy: request.SortBy,
                SortOrder: request.SortOrder
            );

            Result<PaginatedResponse<AttendanceLogResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.AttendanceLogs)
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
         string[] allowedRoles = ["Admin", "Employee", "Manager", "SuperAdmin", "SecurityOfficer", "OrgSupervisor", "SiteSupervisor"];
         return allowedRoles.Contains(userRole, StringComparer.OrdinalIgnoreCase);
     }))
     .RequireRateLimiting("per-user");
    }
}
