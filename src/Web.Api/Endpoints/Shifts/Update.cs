using Application.Abstractions.Messaging;
using Application.Features.Organizations.Shifts.Update;
using Domain.Enums;
using Infrastructure.Authentication;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Shifts;

internal sealed class Update : IEndpoint
{
    public sealed class Request
    {
        public string Name { get; set; } = string.Empty;
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
        public string? ShiftType { get; set; }
        public bool? IsActive { get; set; }
        public string? Description { get; set; }
        public int? GracePeriodMinutes { get; set; }
        public int? MaxLateMinutes { get; set; }
        public bool AllowEarlyCheckIn { get; set; }
        public bool AllowLateCheckOut { get; set; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("shifts/{id:guid}", async (
            Guid id,
            [FromBody] Request request,
            ICommandHandler<UpdateShiftCommand, bool> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateShiftCommand
            {
                ShiftId = id,
                Name = request.Name,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                ShiftType = request.ShiftType is not null ? Enum.Parse<ShiftType>(request.ShiftType) : null,
                IsActive = request.IsActive,
                Description = request.Description,
                GracePeriodMinutes = request.GracePeriodMinutes,
                MaxLateMinutes = request.MaxLateMinutes,
                AllowEarlyCheckIn = request.AllowEarlyCheckIn,
                AllowLateCheckOut = request.AllowLateCheckOut
            };

            Result<bool> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Shifts)
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
