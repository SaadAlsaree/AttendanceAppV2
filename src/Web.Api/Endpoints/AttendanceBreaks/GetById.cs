using Application.Abstractions.Messaging;
using Application.Attendance.AttendanceBreaks.GetById;
using Application.Attendance.AttendanceBreaks.Get;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.AttendanceBreaks;

internal sealed class GetById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("attendance-breaks/{id}", async (
            Guid id,
            IQueryHandler<GetAttendanceBreakByIdQuery, AttendanceBreakResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetAttendanceBreakByIdQuery
            {
                AttendanceBreakId = id
            };

            Result<AttendanceBreakResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.AttendanceBreaks)
        .RequireAuthorization();
    }
}
