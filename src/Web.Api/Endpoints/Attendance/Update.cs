using Application.Abstractions.Messaging;
using Application.Attendance.Update;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Attendance;

internal sealed class Update : IEndpoint
{
    public sealed class Request
    {
        public DateTime? CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public Guid? CheckInWorkLocationId { get; set; }
        public Guid? CheckOutWorkLocationId { get; set; }
        public string? Notes { get; set; }
        public string? Status { get; set; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("attendance/{id:guid}", async (
            Guid id,
            [FromBody] Request request,
            ICommandHandler<UpdateAttendanceCommand, AttendanceResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateAttendanceCommand
            {
                AttendanceId = id,
                CheckInTime = request.CheckInTime,
                CheckOutTime = request.CheckOutTime,
                Notes = request.Notes,
                Status = request.Status != null ? Enum.Parse<Domain.Enums.AttendanceStatus>(request.Status) : null
            };

            Result<AttendanceResponse> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Attendance)
        .RequireAuthorization();
    }
}
