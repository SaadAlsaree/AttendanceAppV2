using Application.Abstractions.Messaging;
using Application.Features.Attendance.AttendanceSchedules.UpdateScheduleDays;
using Infrastructure.Authentication;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Attendance.AttendanceSchedules;

internal sealed class UpdateScheduleDays : IEndpoint
{
    public sealed class Request
    {
        public Guid AttendanceScheduleId { get; set; }
        public List<UpdateScheduleDayRequest> ScheduleDays { get; set; } = new List<UpdateScheduleDayRequest>();
    }

    public sealed class UpdateScheduleDayRequest
    {
        public Guid Id { get; set; }
        public Guid ShiftId { get; set; }
        public bool IsActive { get; set; }
        public string? Notes { get; set; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("attendance-schedules/{attendanceScheduleId}/schedule-days", async (
            Guid attendanceScheduleId,
            Request request,
            ICommandHandler<UpdateScheduleDaysCommand, bool> handler,
            CancellationToken cancellationToken) =>
        {
            request.AttendanceScheduleId = attendanceScheduleId;

            var command = new UpdateScheduleDaysCommand
            {
                AttendanceScheduleId = request.AttendanceScheduleId,
                ScheduleDays = request.ScheduleDays.Select(sd => new UpdateScheduleDayCommand
                {
                    Id = sd.Id,
                    ShiftId = sd.ShiftId,
                    IsActive = sd.IsActive,
                    Notes = sd.Notes
                }).ToList()
            };

            Result<bool> result = await handler.Handle(command, cancellationToken);

            return result.Match(
                success => Results.Ok(new { success = true, message = "Schedule days updated successfully" }),
                error => CustomResults.Problem(error));
        })
        .WithName("UpdateScheduleDays")
        .WithTags(Tags.AttendanceSchedules)
        .WithOpenApi()
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
         string[] allowedRoles = ["Admin", "SuperAdmin", "OrgSupervisor"];
         return allowedRoles.Contains(userRole, StringComparer.OrdinalIgnoreCase);
     }))
     .RequireRateLimiting("per-user");
    }
}
