using Application.Abstractions.Messaging;
using Application.Features.Organizations.Employees.AssignWeeklyShifts;
using Infrastructure.Authentication;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Employees;

internal sealed class AssignWeeklyShifts : IEndpoint
{
    public sealed class Request
    {
        public List<DayShift> Days { get; set; } = new List<DayShift>();

        public sealed class DayShift
        {
            public int DayOfWeek { get; set; }
            public Guid ShiftId { get; set; }
        }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("employees/{id:guid}/weekly-shifts", async (
            Guid id,
            [FromBody] Request request,
            ICommandHandler<AssignWeeklyShiftsCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new AssignWeeklyShiftsCommand
            {
                Id = id,
                Days = request.Days.Select(d => new WeeklyShiftDay
                {
                    DayOfWeek = d.DayOfWeek,
                    ShiftId = d.ShiftId
                }).ToList()
            };

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Employees)
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
