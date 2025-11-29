using Application.Abstractions.Messaging;
using Application.Features.Users.ChangePassword;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

internal sealed class ChangePassword : IEndpoint
{
    public sealed class Request
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users/change-password", async (
           [FromBody] Request request,
            ICommandHandler<ChangePasswordCommand, ApiResponse<bool>> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new ChangePasswordCommand
            {
                CurrentPassword = request.CurrentPassword,
                NewPassword = request.NewPassword,
                ConfirmPassword = request.ConfirmPassword
            };

            Result<ApiResponse<bool>> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Users)
        .RequireAuthorization();
    }
}
