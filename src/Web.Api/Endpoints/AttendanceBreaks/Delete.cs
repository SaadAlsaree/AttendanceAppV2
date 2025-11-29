using Application.Abstractions.Messaging;
using Application.Attendance.AttendanceBreaks.Delete;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.AttendanceBreaks;

internal sealed class Delete : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("attendance-breaks/{id}", async (
            Guid id,
            ICommandHandler<DeleteAttendanceBreakCommand, bool> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new DeleteAttendanceBreakCommand
            {
                AttendanceBreakId = id
            };

            Result<bool> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.AttendanceBreaks)
        .RequireAuthorization();
    }
}
