using Application.Abstractions.Messaging;
using Application.Attendance.Create;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Attendance;

internal sealed class Create : IEndpoint
{
    public sealed class Request
    {
        public Guid EmployeeId { get; set; }
        public Guid OrganizationId { get; set; }
        public DateTime Date { get; set; }
        public Guid? ShiftId { get; set; }
        public Guid? AttendanceScheduleId { get; set; }
        public string? Notes { get; set; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("attendance", async (
            [FromBody] Request request,
            ICommandHandler<CreateAttendanceCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateAttendanceCommand
            {
                EmployeeId = request.EmployeeId,
                OrganizationId = request.OrganizationId,
                Date = request.Date,
                ShiftId = request.ShiftId,
                AttendanceScheduleId = request.AttendanceScheduleId,
                Notes = request.Notes
            };

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Attendance)
        .RequireAuthorization();
    }
}
