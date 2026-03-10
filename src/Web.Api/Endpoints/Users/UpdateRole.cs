using Application.Abstractions.Messaging;
using Application.Features.Users.UpdateRole;
using Domain.Enums;
using Infrastructure.Authentication;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

internal sealed class UpdateRole : IEndpoint
{
    public sealed class Request
    {
        public Role NewRole { get; set; }
        public Guid UpdatedBy { get; set; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("users/{id:guid}/role", async (
            Guid id,
            [FromBody] Request request,
            ICommandHandler<UpdateUserRoleCommand, ApiResponse<bool>> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateUserRoleCommand
            {
                UserId = id,
                NewRole = request.NewRole,
                UpdatedBy = request.UpdatedBy
            };

            Result<ApiResponse<bool>> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
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
         string[] allowedRoles = ["Admin", "SuperAdmin"];
         return allowedRoles.Contains(userRole, StringComparer.OrdinalIgnoreCase);
     }))
     .RequireRateLimiting("per-user");
    }
}
