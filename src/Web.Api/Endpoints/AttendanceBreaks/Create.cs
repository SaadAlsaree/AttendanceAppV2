using Application.Abstractions.Messaging;
using Application.Attendance.AttendanceBreaks.Create;
using Infrastructure.Authentication;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.AttendanceBreaks;

internal sealed class Create : IEndpoint
{
    public sealed class Request
    {
        public Guid AttendanceId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int? DurationMinutes { get; set; }
        public string BreakType { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("attendance-breaks", async (
            [FromBody] Request request,
            ICommandHandler<CreateAttendanceBreakCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateAttendanceBreakCommand
            {
                AttendanceId = request.AttendanceId,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                DurationMinutes = request.DurationMinutes,
                BreakType = Enum.Parse<Domain.Enums.BreakType>(request.BreakType),
                Notes = request.Notes
            };

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.AttendanceBreaks)
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
         string[] allowedRoles = ["Admin", "Employee", "Manager", "SuperAdmin"];
         return allowedRoles.Contains(userRole, StringComparer.OrdinalIgnoreCase);
     }))
     .RequireRateLimiting("per-user");
    }
}
