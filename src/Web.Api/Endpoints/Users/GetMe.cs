using Application.Abstractions.Authentication;
using Application.Models;
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
        .RequireAuthorization();
    }
}
