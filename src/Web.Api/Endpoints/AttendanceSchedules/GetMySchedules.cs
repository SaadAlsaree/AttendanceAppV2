using Application.Abstractions.Messaging;
using Application.Attendance.AttendanceSchedules.GetMySchedules;
using Domain.Enums;
using Infrastructure.Authentication;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Attendance.AttendanceSchedules;

internal sealed class GetMySchedules : IEndpoint
{
    public sealed class Request
    {
        public int? Page { get; set; } = 1;
        public int? PageSize { get; set; } = 10;
        public string? ScheduleType { get; set; }
        public bool? IsActive { get; set; }
        public string? SortBy { get; set; } = "CreatedAt";
        public string? SortOrder { get; set; } = "Descending";
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("attendance-schedules/my-schedules", async (
            [AsParameters] Request request,
            IQueryHandler<GetMySchedulesQuery, PaginatedResponse<AttendanceScheduleResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetMySchedulesQuery
            {
                Page = request.Page ?? 1,
                PageSize = request.PageSize ?? 10,
                ScheduleType = request.ScheduleType is not null ? Enum.Parse<ScheduleType>(request.ScheduleType) : null,
                IsActive = request.IsActive,
                SortBy = request.SortBy,
                SortOrder = request.SortOrder
            };

            Result<PaginatedResponse<AttendanceScheduleResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.AttendanceSchedules)
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
         string[] allowedRoles = ["Admin", "Employee", "Manager", "SuperAdmin", "User", "OrgSupervisor"];
         return allowedRoles.Contains(userRole, StringComparer.OrdinalIgnoreCase);
     }))
     .RequireRateLimiting("per-user");
    }
}
