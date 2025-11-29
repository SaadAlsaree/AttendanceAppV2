using Application.Abstractions.Messaging;
using Application.Attendance.Leaves.Create;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Leaves;

internal sealed class Create : IEndpoint
{
    public sealed class Request
    {
        public Guid EmployeeId { get; set; }
        public LeaveType LeaveType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Reason { get; set; } = string.Empty;
        public Guid? ManagerId { get; set; }
        public string? EmergencyContact { get; set; }
        public string? Notes { get; set; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("leaves", async (
            [FromBody] Request request,
            ICommandHandler<CreateLeaveCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateLeaveCommand
            {
                EmployeeId = request.EmployeeId,
                LeaveType = request.LeaveType,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Reason = request.Reason,
                ManagerId = request.ManagerId,
                EmergencyContact = request.EmergencyContact,
                Notes = request.Notes
            };

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Leaves)
        .RequireAuthorization();
    }
}
