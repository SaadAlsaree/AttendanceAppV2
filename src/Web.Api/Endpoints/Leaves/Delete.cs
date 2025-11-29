using Application.Abstractions.Messaging;
using Application.Attendance.Leaves.Delete;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Leaves;

internal sealed class Delete : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("leaves/{id}", async (
            Guid id,
            ICommandHandler<DeleteLeaveCommand, bool> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new DeleteLeaveCommand
            {
                LeaveId = id
            };

            Result<bool> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Leaves)
        .RequireAuthorization();
    }
}
