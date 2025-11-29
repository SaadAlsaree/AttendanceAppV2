using Application.Abstractions.Messaging;
using Application.Attendance.AttendanceBreaks.Update;
using Application.Attendance.AttendanceBreaks.Get;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.AttendanceBreaks;

internal sealed class Update : IEndpoint
{
    public sealed class Request
    {
        public string? BreakType { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int? DurationMinutes { get; set; }
        public string? Notes { get; set; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("attendance-breaks/{id}", async (
            Guid id,
            [FromBody] Request request,
            ICommandHandler<UpdateAttendanceBreakCommand, AttendanceBreakResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateAttendanceBreakCommand
            {
                AttendanceBreakId = id,
                BreakType = request.BreakType != null ? Enum.Parse<Domain.Enums.BreakType>(request.BreakType) : null,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                DurationMinutes = request.DurationMinutes,
                Notes = request.Notes
            };

            Result<AttendanceBreakResponse> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.AttendanceBreaks)
        .RequireAuthorization();
    }
}
