using Application.Abstractions.Messaging;
using Application.Features.Organizations.EmployeeWeeklyShifts.Get;
using Infrastructure.Authentication;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Employees;

internal sealed class GetWeeklyShifts : IEndpoint
{
    public sealed class Request
    {
        public int? Page { get; set; } = 1;
        public int? PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; } = string.Empty;
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("employees/weekly-shifts", async (
            [AsParameters] Request request,
            IQueryHandler<GetEmployeeWeeklyShiftsQuery, PaginatedResponse<EmployeeWeeklyShiftsResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetEmployeeWeeklyShiftsQuery
            {
                Page = request.Page,
                PageSize = request.PageSize,
                SearchTerm = request.SearchTerm
            };

            Result<PaginatedResponse<EmployeeWeeklyShiftsResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Employees)
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
