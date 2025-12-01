using Application.Abstractions.Messaging;
using Application.Attendance.Leaves.Get;
using Domain.Enums;
using Infrastructure.Authentication;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Leaves;

internal sealed class Get : IEndpoint
{
    public sealed class Request
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public Guid? EmployeeId { get; set; }
        public Guid? ManagerId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? LeaveType { get; set; }
        public string? Status { get; set; }
        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; }
        public string? SortOrder { get; set; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("leaves", async (
            [AsParameters] Request request,
            IQueryHandler<GetLeavesQuery, PaginatedResponse<LeaveResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetLeavesQuery
            {
                Page = request.Page,
                PageSize = request.PageSize,
                EmployeeId = request.EmployeeId,
                ManagerId = request.ManagerId,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                LeaveType = request.LeaveType != null ? Enum.Parse<LeaveType>(request.LeaveType) : null,
                Status = request.Status != null ? Enum.Parse<LeaveStatus>(request.Status) : null,
                SearchTerm = request.SearchTerm,
                SortBy = request.SortBy,
                SortOrder = request.SortOrder != null ? Enum.Parse<SortOrder>(request.SortOrder) : SortOrder.Ascending
            };

            Result<PaginatedResponse<LeaveResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Leaves)
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
     }));
    }
}
