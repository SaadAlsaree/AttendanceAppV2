using Application.Abstractions.Messaging;
using Application.Features.Organizations.Employees.Delete;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Employees;

internal sealed class Delete : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("employees/{id:guid}", async (
            Guid id,
            ICommandHandler<DeleteEmployeeCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new DeleteEmployeeCommand { Id = id };

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Employees)
        .RequireAuthorization();
    }
}
