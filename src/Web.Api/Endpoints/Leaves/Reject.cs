using Application.Abstractions.Messaging;
using Application.Attendance.Leaves.Reject;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Leaves;

internal sealed class Reject : IEndpoint
{
    public sealed class Request
    {
        public Guid RejectedBy { get; set; }
        public string RejectionReason { get; set; } = string.Empty;
        public string? RejectionNotes { get; set; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("leaves/{id}/reject", async (
            Guid id,
         [FromBody] Request request,
            ICommandHandler<RejectLeaveCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new RejectLeaveCommand
            {
                LeaveId = id,
                RejectedBy = request.RejectedBy,
                RejectionReason = request.RejectionReason,
                RejectionNotes = request.RejectionNotes
            };

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Leaves)
        .RequireAuthorization();
    }
}
