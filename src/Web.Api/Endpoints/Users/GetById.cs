using Application.Abstractions.Messaging;
using Application.Features.Users.GetById;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

internal sealed class GetById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("users/{id:guid}", async (
            Guid id,
            IQueryHandler<GetUserByIdQuery, UserResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetUserByIdQuery(id);

            Result<UserResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(
                success => Results.Ok(success),
                CustomResults.Problem
            );
        })
        .WithTags(Tags.Users)
        .RequireAuthorization();
    }
}
