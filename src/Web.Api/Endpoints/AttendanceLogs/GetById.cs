using Application.Abstractions.Messaging;
using Application.Attendance.AttendanceLogs.GetById;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.AttendanceLogs;

internal sealed class GetById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("attendance-logs/{id}", async (
            Guid id,
            IQueryHandler<GetAttendanceLogByIdQuery, AttendanceLogResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetAttendanceLogByIdQuery(id);

            Result<AttendanceLogResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.AttendanceLogs)
        .RequireAuthorization();
    }
}
