using Application.Abstractions.Messaging;
using Application.Features.Users.Login;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Auth;

internal sealed class Login : IEndpoint
{
    public sealed class Request
    {
        public string UserLogin { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("auth/login", async (
            [FromBody] Request request,
            ICommandHandler<LoginCommand, ApiResponse<LoginResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            // Debug logging
            Console.WriteLine($"Login request received - UserLogin: '{request.UserLogin}', Password: '{request.Password}'");

            var command = new LoginCommand(request.UserLogin, request.Password);

            Result<ApiResponse<LoginResponse>> result = await handler.Handle(command, cancellationToken);

            return result.Match(
                success => Results.Ok(success),
                CustomResults.Problem
            );
        })
        .WithTags(Tags.Auth)
        .AllowAnonymous();
    }
}
