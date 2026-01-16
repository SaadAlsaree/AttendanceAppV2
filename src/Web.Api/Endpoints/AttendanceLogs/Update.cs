using Application.Abstractions.Messaging;
using Application.Attendance.AttendanceLogs.Update;
using Application.Attendance.AttendanceLogs.GetById;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Infrastructure.Authentication;

namespace Web.Api.Endpoints.AttendanceLogs;

internal sealed class Update : IEndpoint
{
    public sealed class Request
    {
        public DateTime? DateTimeAttend { get; set; }
        public string? CardNo { get; set; }
        public string? EmpID { get; set; }
        public DateOnly? DateWork { get; set; }
        public TimeSpan? TimeAttend { get; set; }
        public int? Direct { get; set; }
        public string? DeviceName { get; set; }
        public string? DeviceNo { get; set; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("attendance-logs/{id}", async (
            Guid id,
            [FromBody] Request request,
            ICommandHandler<UpdateAttendanceLogCommand, AttendanceLogResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateAttendanceLogCommand
            {
                AttendanceLogId = id,
                DateTimeAttend = request.DateTimeAttend,
                CardNo = request.CardNo,
                EmpID = request.EmpID,
                DateWork = request.DateWork,
                TimeAttend = request.TimeAttend,
                Direct = request.Direct,
                DeviceName = request.DeviceName,
                DeviceNo = request.DeviceNo
            };

            Result<AttendanceLogResponse> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.AttendanceLogs)
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
         string[] allowedRoles = ["Admin", "SuperAdmin"];
         return allowedRoles.Contains(userRole, StringComparer.OrdinalIgnoreCase);
     }))
     .RequireRateLimiting("per-user");
    }
}
