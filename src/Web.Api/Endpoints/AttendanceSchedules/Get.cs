using Application.Abstractions.Messaging;
using Application.Attendance.AttendanceSchedules.Get;
using Domain.Enums;
using Infrastructure.Authentication;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Attendance.AttendanceSchedules;

internal sealed class Get : IEndpoint
{
    public sealed class Request
    {
        public int? Page { get; set; } = 1;
        public int? PageSize { get; set; } = 10;
        public Guid? EmployeeId { get; set; }
        public string? ScheduleType { get; set; }
        public bool? IsActive { get; set; }
        public string? SearchTerm { get; set; } = string.Empty;
        public string? SortBy { get; set; } = "CreatedAt";
        public string? SortOrder { get; set; } = "Descending";
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("attendance-schedules", async (
            [AsParameters] Request request,
            IQueryHandler<GetAttendanceSchedulesQuery, PaginatedResponse<AttendanceScheduleResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetAttendanceSchedulesQuery
            {
                Page = request.Page ?? 1,
                PageSize = request.PageSize ?? 10,
                EmployeeId = request.EmployeeId,
                ScheduleType = request.ScheduleType is not null ? Enum.Parse<ScheduleType>(request.ScheduleType) : null,
                IsActive = request.IsActive,
                SearchTerm = request.SearchTerm,
                SortBy = request.SortBy,
                SortOrder = request.SortOrder is not null
                    ? Enum.Parse<SortOrder>(request.SortOrder, ignoreCase: true)
                    : null
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
         string[] allowedRoles = ["Admin", "Employee", "Manager", "SuperAdmin", "OrgSupervisor"];
         return allowedRoles.Contains(userRole, StringComparer.OrdinalIgnoreCase);
     }))
     .RequireRateLimiting("per-user");
    }
}
