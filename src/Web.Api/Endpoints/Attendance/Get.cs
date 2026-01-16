using Application.Abstractions.Messaging;
using Application.Attendance.Get;
using Infrastructure.Authentication;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Attendance;

internal sealed class Get : IEndpoint
{
    public sealed class Request
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public Guid? EmployeeId { get; set; }
        public Guid? OrganizationId { get; set; }
        public DateOnly? Date { get; set; }
        public string? Status { get; set; }
        public Guid? ShiftId { get; set; }
        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; }
        public string? SortOrder { get; set; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("attendance", async (
            [AsParameters] Request request,
            IQueryHandler<GetAttendanceQuery, PaginatedResponse<AttendanceResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetAttendanceQuery
            {
                Page = request.Page,
                PageSize = request.PageSize,
                EmployeeId = request.EmployeeId,
                OrganizationId = request.OrganizationId,
                Date = request.Date,
                Status = request.Status != null ? Enum.Parse<Domain.Enums.AttendanceStatus>(request.Status) : null,
                ShiftId = request.ShiftId,
                SearchTerm = request.SearchTerm,
                SortBy = request.SortBy,
                SortOrder = request.SortOrder
            };

            Result<PaginatedResponse<AttendanceResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Attendance)
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
     .RequireRateLimiting("per-user");
    }
}
