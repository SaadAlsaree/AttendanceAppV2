using Application.Abstractions.Messaging;
using Application.Attendance.Delete;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Attendance;

internal sealed class Delete : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("attendance/{id:guid}", async (
            Guid id,
            ICommandHandler<DeleteAttendanceCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new DeleteAttendanceCommand { AttendanceId = id };
            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Attendance)
        .RequireAuthorization();
    }
}
