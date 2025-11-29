using Application.Abstractions.Messaging;
using Application.Features.Users.UpdateRole;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Employees;

internal sealed class UpdateRole : IEndpoint
{
    public sealed class Request
    {
        public Guid UserId { get; set; }
        public Role NewRole { get; set; }  // Note: It's NewRole, not Role
        public Guid UpdatedBy { get; set; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("employees/{id:guid}/role", async (
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
        .WithTags(Tags.Employees)
        .RequireAuthorization();
    }
}
