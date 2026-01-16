using Application.Abstractions.Messaging;
using Application.Attendance.AttendanceBreaks.GetById;
using Application.Attendance.AttendanceBreaks.Get;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;
using Infrastructure.Authentication;

namespace Web.Api.Endpoints.AttendanceBreaks;

internal sealed class GetById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("attendance-breaks/{id}", async (
            Guid id,
            IQueryHandler<GetAttendanceBreakByIdQuery, AttendanceBreakResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetAttendanceBreakByIdQuery
            {
                AttendanceBreakId = id
            };

            Result<AttendanceBreakResponse> result = await handler.Handle(query, cancellationToken);

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
