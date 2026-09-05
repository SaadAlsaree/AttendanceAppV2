using Application.Abstractions.Messaging;
using Application.Features.Users.SignUp;
using Domain.Enums;
using Infrastructure.Authentication;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

internal sealed class SignUp : IEndpoint
{
    public sealed class Request
    {
        public string Username { get; set; } = string.Empty;
        public string UserLogin { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public Role Role { get; set; } = Role.User;
        public Guid OrganizationalUnitId { get; set; }

        /// <summary>Required when Role is SiteSupervisor.</summary>
        public Guid? SiteId { get; set; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users/new", async (
            [FromBody] Request request,
            ICommandHandler<SignUpCommand, ApiResponse<SignUpResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new SignUpCommand(
                request.Username,
                request.UserLogin,
                request.Password,
                request.ConfirmPassword,
                request.Role,
                request.OrganizationalUnitId,
                request.SiteId);

            Result<ApiResponse<SignUpResponse>> result = await handler.Handle(command, cancellationToken);

            return result.Match(
                success => Results.Ok(success),
                CustomResults.Problem
            );
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
