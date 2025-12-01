using Application.Abstractions.Messaging;
using Application.Attendance.CheckOut;
using Infrastructure.Authentication;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Attendance;

internal sealed class CheckOut : IEndpoint
{
    public sealed class Request
    {
        public Guid EmployeeId { get; set; }
        public Guid AttendanceId { get; set; }
        public DateTime CheckOutTime { get; set; }
        public int? Major { get; set; }
        public int? Minor { get; set; }
        public string CardNo { get; set; } = string.Empty;
        public int? CardType { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? CardReaderNo { get; set; }
        public int? DoorNo { get; set; }
        public string? EmployeeNoString { get; set; }
        public int? SerialNo { get; set; }
        public string? UserType { get; set; }
        public string? CurrentVerifyMode { get; set; }
        public string? AttendanceStatus { get; set; } = "checkOut";
        public string? Label { get; set; }
        public string? Mask { get; set; }
        public string? PictureURL { get; set; }
        public string? Notes { get; set; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("attendance/check-out", async (
            [FromBody] Request request,
            ICommandHandler<CheckOutCommand, AttendanceResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CheckOutCommand
            {
                EmployeeId = request.EmployeeId,
                AttendanceId = request.AttendanceId,
                CheckOutTime = request.CheckOutTime,
                Major = request.Major,
                Minor = request.Minor,
                CardNo = request.CardNo,
                CardType = request.CardType,
                Name = request.Name,
                CardReaderNo = request.CardReaderNo,
                DoorNo = request.DoorNo,
                EmployeeNoString = request.EmployeeNoString,
                SerialNo = request.SerialNo,
                UserType = request.UserType,
                CurrentVerifyMode = request.CurrentVerifyMode,
                AttendanceStatus = request.AttendanceStatus,
                Label = request.Label,
                Mask = request.Mask,
                PictureURL = request.PictureURL,
                Notes = request.Notes
            };

            Result<AttendanceResponse> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Attendance)
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
     }));
    }
}
