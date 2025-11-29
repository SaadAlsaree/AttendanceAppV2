using Application.Abstractions.Messaging;
using Application.Attendance.AttendanceLogs.Delete;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.AttendanceLogs;

internal sealed class Delete : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("attendance-logs/{id}", async (
            Guid id,
            ICommandHandler<DeleteAttendanceLogCommand, bool> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new DeleteAttendanceLogCommand { AttendanceLogId = id };

            Result<bool> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.AttendanceLogs)
        .RequireAuthorization();
    }
}
