using Application.Abstractions.Messaging;
using Application.Features.Organizations.Shifts.GetById;
using Application.Features.Organizations.Shifts.Get;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;
using Infrastructure.Authentication;

namespace Web.Api.Endpoints.Shifts;

internal sealed class GetById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("shifts/{id:guid}", async (
            Guid id,
            IQueryHandler<GetShiftByIdQuery, ApiResponse<ShiftResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetShiftByIdQuery { ShiftId = id };
            Result<ApiResponse<ShiftResponse>> result = await handler.Handle(query, cancellationToken);

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
         string[] allowedRoles = ["Admin", "Employee", "Manager", "SuperAdmin", "SecurityOfficer"];
         return allowedRoles.Contains(userRole, StringComparer.OrdinalIgnoreCase);
     }))
     .RequireRateLimiting("per-user");
    }
}
