using Application.Abstractions.Messaging;
using Application.Features.Users.Get;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

internal sealed class Get : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("users", async (
            [AsParameters] GetUsersQuery query,
            IQueryHandler<GetUsersQuery, PaginatedResponse<UserResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            Result<PaginatedResponse<UserResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(
                success => Results.Ok(success),
                CustomResults.Problem
            );
        })
        .WithTags(Tags.Users)
        .RequireAuthorization();
    }
}
