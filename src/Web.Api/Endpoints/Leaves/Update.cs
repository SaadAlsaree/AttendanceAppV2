using Application.Abstractions.Messaging;
using Application.Attendance.Leaves.Update;
using Domain.Enums;
using Infrastructure.Authentication;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Leaves;

internal sealed class Update : IEndpoint
{
    public sealed class Request
    {
        public LeaveType? LeaveType { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Reason { get; set; }
        public string? EmergencyContact { get; set; }
        public string? Notes { get; set; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("leaves/{id}", async (
            Guid id,
            [FromBody] Request request,
            ICommandHandler<UpdateLeaveCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateLeaveCommand
            {
                LeaveId = id,
                LeaveType = request.LeaveType,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Reason = request.Reason,
                EmergencyContact = request.EmergencyContact,
                Notes = request.Notes
            };

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Leaves)
         .RequireAuthorization(policy => policy
     .RequireAssertion(context =>
     {
         if (context.User.Identity?.IsAuthenticated != true)
         {
             return false;
         }

         // الحصول على Role من JWT Token Claims
         string? userRole = context.User.GetRole();

         if (string.IsNullOrWhiteSpace(userRole))
         {
             return false;
         }

         // OR logic: إذا كان لديه أي Role من الأدوار المطلوبة
         string[] allowedRoles = ["Admin", "SuperAdmin", "Employee", "OrgSupervisor"];
         return allowedRoles.Contains(userRole, StringComparer.OrdinalIgnoreCase);
     }))
     .RequireRateLimiting("per-user");
    }
}
