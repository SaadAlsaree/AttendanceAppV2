using System.Globalization;
using System.Text.Json.Serialization;
using Application.Abstractions.Messaging;
using Application.Attendance.AttendanceSchedules.Update;
using Domain.Enums;
using Infrastructure.Authentication;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Attendance.AttendanceSchedules;

internal sealed class Update : IEndpoint
{
    public sealed class Request
    {
        public string StartDate { get; set; } = string.Empty;
        public string? EndDate { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ScheduleType? ScheduleType { get; set; }
        public bool IsActive { get; set; }
        public string? Notes { get; set; }
        public List<ScheduleDayRequest>? ScheduleDays { get; set; }
        public List<string>? ExcludedDates { get; set; }

        /// <summary>
        /// Indicates whether ScheduleDays should be updated.
        /// If false, ScheduleDays will remain unchanged.
        /// If true, ScheduleDays will be updated based on the ScheduleDays property value.
        /// </summary>
        public bool UpdateScheduleDays { get; set; }
    }

    public sealed class ScheduleDayRequest
    {
        public Guid? Id { get; set; }
        public Guid? AttendanceScheduleId { get; set; }
        public string? ScheduleDayDate { get; set; }
        public Guid? ShiftId { get; set; }
        public bool? IsActive { get; set; } = true;
        public string? Notes { get; set; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("attendance-schedules/{id:guid}", async (
            Guid id,
            Request request,
            ICommandHandler<UpdateAttendanceScheduleCommand, AttendanceScheduleResponse> handler,
            CancellationToken cancellationToken) =>
        {
            DateOnly? startDate = !string.IsNullOrEmpty(request.StartDate)
                ? DateOnly.Parse(request.StartDate, CultureInfo.InvariantCulture)
                : null;
            DateOnly? endDate = !string.IsNullOrEmpty(request.EndDate)
                ? DateOnly.Parse(request.EndDate, CultureInfo.InvariantCulture)
                : null;

            var excludedDates = request.ExcludedDates?.Select(date => DateOnly.Parse(date, CultureInfo.InvariantCulture)).ToList();

            var scheduleDays = request.ScheduleDays?.Select(sd => new UpdateScheduleDayCommand
            {
                Id = sd.Id,
                AttendanceScheduleId = sd.AttendanceScheduleId,
                ScheduleDayDate = !string.IsNullOrEmpty(sd.ScheduleDayDate)
                    ? DateOnly.Parse(sd.ScheduleDayDate, CultureInfo.InvariantCulture)
                    : null,
                ShiftId = sd.ShiftId,
                IsActive = sd.IsActive,
                Notes = sd.Notes
            }).ToList();

            var command = new UpdateAttendanceScheduleCommand
            {
                AttendanceScheduleId = id,
                StartDate = startDate,
                EndDate = endDate,
                ScheduleType = request.ScheduleType ?? ScheduleType.Regular,
                IsActive = request.IsActive,
                Notes = request.Notes,
                ExcludedDates = excludedDates,
                ScheduleDays = scheduleDays,
                UpdateScheduleDays = request.UpdateScheduleDays
            };

            Result<AttendanceScheduleResponse> result = await handler.Handle(command, cancellationToken);

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
         string[] allowedRoles = ["Admin", "SuperAdmin", "Employee"];
         return allowedRoles.Contains(userRole, StringComparer.OrdinalIgnoreCase);
     }));
    }
}
