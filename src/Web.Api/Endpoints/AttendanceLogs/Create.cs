using Application.Abstractions.Messaging;
using Application.Attendance.AttendanceLogs.Create;
using Infrastructure.Authentication;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.AttendanceLogs;

internal sealed class Create : IEndpoint
{
    public sealed class Request
    {
        public Guid EmployeeId { get; set; }
        public Guid? OrganizationId { get; set; }
        public Guid? AttendanceId { get; set; }
        public DateTime DateTimeAttend { get; set; }
        public string CardNo { get; set; } = string.Empty;
        public string EmpID { get; set; } = string.Empty;
        public DateOnly DateWork { get; set; }
        public TimeSpan? TimeAttend { get; set; }  // Assuming this stores only time
        public int Direct { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public string DeviceNo { get; set; } = string.Empty;
        public string EmpName { get; set; } = string.Empty;
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("attendance-logs", async (
            [FromBody] Request request,
            ICommandHandler<CreateAttendanceLogCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateAttendanceLogCommand
            {
                DateTimeAttend = request.DateTimeAttend,
                CardNo = request.CardNo,
                EmpID = request.EmpID,
                DateWork = request.DateWork,
                TimeAttend = request.TimeAttend,
                Direct = request.Direct,
                DeviceName = request.DeviceName,
                DeviceNo = request.DeviceNo,
                EmpName = request.EmpName
            };

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(
                id => Results.Created($"/attendance-logs/{id}", id),
                CustomResults.Problem);
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
         string[] allowedRoles = ["Admin", "Employee", "Manager", "SuperAdmin"];
         return allowedRoles.Contains(userRole, StringComparer.OrdinalIgnoreCase);
     }));
    }
}
