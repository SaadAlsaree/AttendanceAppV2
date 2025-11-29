using Application.Abstractions.Messaging;
using Application.Attendance.AttendanceSchedules.GetById;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Attendance.AttendanceSchedules;

internal sealed class GetById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("attendance-schedules/{id:guid}", async (
            Guid id,
            IQueryHandler<GetAttendanceScheduleByIdQuery, ApiResponse<AttendanceScheduleResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetAttendanceScheduleByIdQuery { AttendanceScheduleId = id };
            Result<ApiResponse<AttendanceScheduleResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.AttendanceSchedules)
        .RequireAuthorization();
    }
}
