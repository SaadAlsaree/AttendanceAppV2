using Application.Abstractions.Messaging;
using Application.Features.Users.ResetPassword;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

internal sealed class ResetPassword : IEndpoint
{
    public sealed class Request
    {
        public Guid UserId { get; set; }
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users/reset-password", async (
            [FromBody] Request request,
            ICommandHandler<ResetPasswordCommand, bool> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new ResetPasswordCommand(
                request.UserId,
                request.NewPassword,
                request.ConfirmPassword);

            Result<bool> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Users)
        .AllowAnonymous();
    }
}
