using System.Globalization;
using Application.Abstractions.Messaging;
using Application.Features.Attendance.AttendanceSchedules.BulkInsert;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Attendance.AttendanceSchedules;

internal sealed class BulkInsert : IEndpoint
{
    public sealed class Request
    {
        public string StartDate { get; set; } = string.Empty;
        public string? EndDate { get; set; }
        public Guid ShiftId { get; set; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("attendance-schedules/bulk", async (
            [FromBody] Request request,
            ICommandHandler<BulkInsertAttendanceScheduleCommand, bool> handler,
            CancellationToken cancellationToken) =>
        {
            var startDate = DateOnly.Parse(request.StartDate, CultureInfo.InvariantCulture);
            DateOnly? endDate = !string.IsNullOrEmpty(request.EndDate)
                ? DateOnly.Parse(request.EndDate, CultureInfo.InvariantCulture)
                : null;

            var command = new BulkInsertAttendanceScheduleCommand
            {
                StartDate = startDate,
                EndDate = endDate,
                ShiftId = request.ShiftId
            };

            Result<bool> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.AttendanceSchedules)
        .RequireAuthorization();
    }
}
