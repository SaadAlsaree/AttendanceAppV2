using Application.Abstractions.Authentication;
using Application.Models;
using Infrastructure.Authentication;
using Microsoft.AspNetCore.Mvc;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

internal sealed class GetMe : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("users/me", async (
            IUserContext userContext,
            CancellationToken cancellationToken) =>
        {
            UserInfoDto userInfo = await userContext.GetUserAsync();

            return Results.Ok(userInfo);
        })
        .WithTags(Tags.Users)
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
         string[] allowedRoles = ["Admin", "Employee", "Manager", "SuperAdmin", "SecurityOfficer", "OrgSupervisor"];
         return allowedRoles.Contains(userRole, StringComparer.OrdinalIgnoreCase);
     }))
     .RequireRateLimiting("per-user");
    }
}
