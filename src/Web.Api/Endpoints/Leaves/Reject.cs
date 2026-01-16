using Application.Abstractions.Messaging;
using Application.Attendance.Leaves.Reject;
using Infrastructure.Authentication;
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
         string[] allowedRoles = ["Admin", "SuperAdmin", "Manager"];
         return allowedRoles.Contains(userRole, StringComparer.OrdinalIgnoreCase);
     }))
     .RequireRateLimiting("per-user");
    }
}
