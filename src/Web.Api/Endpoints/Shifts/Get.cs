using Application.Abstractions.Messaging;
using Application.Features.Organizations.Shifts.Get;
using Infrastructure.Authentication;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Shifts;

internal sealed class Get : IEndpoint
{
    public sealed class Request
    {
        public int? Page { get; set; } = 1;
        public int? PageSize { get; set; } = 10;
        public Guid? OrganizationId { get; set; }
        public string? ShiftType { get; set; }
        public bool? IsActive { get; set; }
        public string? SearchTerm { get; set; } = string.Empty;
        public string? SortBy { get; set; } = "Name";
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("shifts", async (
            [AsParameters] Request request,
            IQueryHandler<GetShiftsQuery, PaginatedResponse<ShiftResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetShiftsQuery
            {
                Page = request.Page,
                PageSize = request.PageSize,
                ShiftType = request.ShiftType is not null ? Enum.Parse<Domain.Enums.ShiftType>(request.ShiftType) : null,
                IsActive = request.IsActive,
                SearchTerm = request.SearchTerm,
                SortBy = request.SortBy
            };

            Result<PaginatedResponse<ShiftResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Shifts)
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
