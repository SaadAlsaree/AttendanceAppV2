using Application.Abstractions.Messaging;
using Application.Features.Users.Update;
using Domain.Enums;
using Infrastructure.Authentication;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

internal sealed class Update : IEndpoint
{
    public sealed class Request
    {
        public string Username { get; set; } = string.Empty;
        public string UserLogin { get; set; } = string.Empty;
        public Role Role { get; set; }
        public UserStatus Status { get; set; }
        public bool IsActive { get; set; }
        public Guid? OrganizationalUnitId { get; set; }

        /// <summary>Required when Role is SiteSupervisor. Always send it: the handler assigns
        /// unconditionally, so omitting it clears the user's site.</summary>
        public Guid? SiteId { get; set; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("users/{id:guid}", async (
            Guid id,
            [FromBody] Request request,
            ICommandHandler<UpdateUserCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateUserCommand
            {
                Id = id,
                Username = request.Username,
                UserLogin = request.UserLogin,
                Role = request.Role,
                Status = request.Status,
                IsActive = request.IsActive,
                OrganizationalUnitId = request.OrganizationalUnitId,
                SiteId = request.SiteId
            };

            Result result = await handler.Handle(command, cancellationToken);

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

