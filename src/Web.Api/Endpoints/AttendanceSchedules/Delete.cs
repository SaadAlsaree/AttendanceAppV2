using Application.Abstractions.Messaging;
using Application.Attendance.AttendanceSchedules.Delete;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Attendance.AttendanceSchedules;

internal sealed class Delete : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("attendance-schedules/{id:guid}", async (
            Guid id,
            ICommandHandler<DeleteAttendanceScheduleCommand, bool> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new DeleteAttendanceScheduleCommand { AttendanceScheduleId = id };
            Result<bool> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.AttendanceSchedules)
        .RequireAuthorization();
    }
}
