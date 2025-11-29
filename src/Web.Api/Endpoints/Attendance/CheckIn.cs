using Application.Abstractions.Messaging;
using Application.Attendance.CheckIn;
using Domain.Entities.Attendance;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Attendance;

internal sealed class CheckIn : IEndpoint
{
    public sealed class Request
    {
        public Guid AttendanceLogId { get; set; }
        public Guid EmployeeId { get; set; }
        public Guid OrganizationId { get; set; }
        public LogMethod? CheckInMethod { get; set; }
        public LogMethod? CheckOutMethod { get; set; }
        public string? Notes { get; set; }
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
        app.MapPost("attendance/check-in", async (
            [FromBody] Request request,
            ICommandHandler<CheckInCommand, AttendanceResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CheckInCommand
            {
                AttendanceLogId = request.AttendanceLogId,
                EmployeeId = request.EmployeeId,
                OrganizationId = request.OrganizationId,
                CheckInMethod = request.CheckInMethod,
                CheckOutMethod = request.CheckOutMethod,
                CardNo = request.CardNo,
                EmpID = request.EmpID,
                DateWork = request.DateWork,
                TimeAttend = request.TimeAttend,
                Direct = request.Direct,
                DeviceName = request.DeviceName,
                DeviceNo = request.DeviceNo,
                EmpName = request.EmpName,
                Notes = request.Notes,
                DateTimeAttend = request.DateTimeAttend
            };

            Result<AttendanceResponse> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Attendance)
        .RequireAuthorization();
    }
}
