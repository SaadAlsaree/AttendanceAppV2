using Application.Abstractions.Messaging;
using Application.Attendance.Leaves.Approve;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Leaves;

internal sealed class Approve : IEndpoint
{
    public sealed class Request
    {
        public Guid ApprovedBy { get; set; }
        public string? ApprovalNotes { get; set; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("leaves/{id}/approve", async (
            Guid id,
            [FromBody] Request request,
            ICommandHandler<ApproveLeaveCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new ApproveLeaveCommand
            {
                LeaveId = id,
                ApprovedBy = request.ApprovedBy,
                ApprovalNotes = request.ApprovalNotes
            };

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Leaves)
        .RequireAuthorization();
    }
}
