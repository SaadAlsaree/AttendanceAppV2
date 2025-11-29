using Application.Abstractions.Authentication;
using Application.Abstractions.Messaging;
using Application.Features.Users.Delete;
using Application.Models;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

internal sealed class Delete : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("users/{id:guid}", async (
            Guid id,
            ICommandHandler<DeleteUserCommand, bool> handler,
            IUserContext userContext,
            CancellationToken cancellationToken) =>
        {
            UserInfoDto currentUser = await userContext.GetUserAsync();

            var command = new DeleteUserCommand(id, currentUser.Id);

            Result<bool> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Users)
        .RequireAuthorization();
    }
}
