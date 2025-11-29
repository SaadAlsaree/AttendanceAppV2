using Application.Abstractions.Messaging;
using Application.Attendance.GetById;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Attendance;

internal sealed class GetById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("attendance/{id:guid}", async (
            Guid id,
            IQueryHandler<GetAttendanceByIdQuery, ApiResponse<AttendanceResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetAttendanceByIdQuery { AttendanceId = id };
            Result<ApiResponse<AttendanceResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Attendance)
        .RequireAuthorization();
    }
}
