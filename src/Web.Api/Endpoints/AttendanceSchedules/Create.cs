using System.Globalization;
using Application.Abstractions.Messaging;
using Application.Attendance.AttendanceSchedules.Create;
using Domain.Enums;
using Infrastructure.Authentication;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Attendance.AttendanceSchedules;

internal sealed class Create : IEndpoint
{
    public sealed class Request
    {
        public Guid EmployeeId { get; set; }
        public string StartDate { get; set; } = string.Empty;
        public string? EndDate { get; set; }
        public string ScheduleType { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string? Notes { get; set; }
        public List<CreateScheduleDayCommand> ScheduleDays { get; set; } = new List<CreateScheduleDayCommand>();
        public List<string> ExcludedDates { get; set; } = new List<string>();
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("attendance-schedules", async (
            [FromBody] Request request,
            ICommandHandler<CreateAttendanceScheduleCommand, bool> handler,
            CancellationToken cancellationToken) =>
        {
            var startDate = DateOnly.Parse(request.StartDate, CultureInfo.InvariantCulture);
            DateOnly? endDate = !string.IsNullOrEmpty(request.EndDate)
                ? DateOnly.Parse(request.EndDate, CultureInfo.InvariantCulture)
                : null;

            var excludedDates = request.ExcludedDates
                .Select(date => DateOnly.Parse(date, CultureInfo.InvariantCulture))
                .ToList();

            var command = new CreateAttendanceScheduleCommand
            {
                EmployeeId = request.EmployeeId,
                StartDate = startDate,
                EndDate = endDate,
                ScheduleType = Enum.Parse<ScheduleType>(request.ScheduleType),
                IsActive = request.IsActive,
                Notes = request.Notes,
                ScheduleDays = request.ScheduleDays,
                ExcludedDates = excludedDates
            };

            Result<bool> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.AttendanceSchedules)
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
