using Application.Abstractions.Messaging;
using Application.Attendance.Approve;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Attendance;

internal sealed class Approve : IEndpoint
{
    public sealed class Request
    {
        public Guid ApprovedBy { get; set; }
        public string? ApprovalNotes { get; set; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("attendance/{id:guid}/approve", async (
            Guid id,
            [FromBody] Request request,
            ICommandHandler<ApproveAttendanceCommand, AttendanceResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new ApproveAttendanceCommand
            {
                AttendanceId = id,
                ApprovedBy = request.ApprovedBy,
                ApprovalNotes = request.ApprovalNotes
            };

            Result<AttendanceResponse> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Attendance)
        .RequireAuthorization();
    }
}
