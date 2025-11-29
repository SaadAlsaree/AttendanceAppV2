using Application.Abstractions.Messaging;
using Application.Features.Organizations.Employees.AssignManager;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Employees;

internal sealed class AssignManager : IEndpoint
{
    public sealed class Request
    {
        public Guid ManagerId { get; set; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("employees/{id:guid}/manager", async (
            Guid id,
            [FromBody] Request request,
            ICommandHandler<AssignManagerCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new AssignManagerCommand
            {
                Id = id,
                ManagerId = request.ManagerId
            };

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Employees)
        .RequireAuthorization();
    }
}
