using Application.Abstractions.Messaging;
using Application.Organizations.Holidays.Get;
using Infrastructure.Authentication;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Holidays;

internal sealed class Get : IEndpoint
{
    public sealed class Request
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public Guid? OrganizationId { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public bool? IsRecurring { get; set; }
        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; }
        public string? SortOrder { get; set; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("holidays", async (
            [AsParameters] Request request,
            IQueryHandler<GetHolidaysQuery, PaginatedResponse<HolidayResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetHolidaysQuery
            {
                Page = request.Page,
                PageSize = request.PageSize,
                OrganizationId = request.OrganizationId,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                IsRecurring = request.IsRecurring,
                SearchTerm = request.SearchTerm,
                SortBy = request.SortBy,
                SortOrder = request.SortOrder
            };

            Result<PaginatedResponse<HolidayResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Holidays).RequireAuthorization(policy => policy
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
