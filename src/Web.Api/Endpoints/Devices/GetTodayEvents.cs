using Application.Abstractions.Data;
using Application.Models;
using Infrastructure.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace Web.Api.Endpoints.Devices;

public class GetTodayEvents : IEndpoint
{
    public sealed class Request
    {
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/devices/today-events", async (
            [FromBody] Request request,
            [FromServices] IHikvisionService hikvisionService) =>
        {
            // استخدام الوقت الحالي إذا لم يتم تحديد وقت البداية والنهاية
            DateTime startTime = request.StartTime ?? DateTime.Today;
            DateTime endTime = request.EndTime ?? DateTime.Today.AddDays(1).AddSeconds(-1);

            HikvisionResponse<AccessLogSearchResult> result = await hikvisionService.GetTodayEventsAsync(startTime, endTime);

            return Results.Ok(result);
        })
        .WithTags(Tags.Devices)
        .WithName("GetTodayEvents")
        .WithOpenApi(operation =>
        {
            operation.Summary = "جلب أحداث اليوم من جميع الأجهزة";
            operation.Description = "يجلب جميع أحداث الحضور والانصراف من الأجهزة المتصلة خلال الفترة المحددة";
            return operation;
        })
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
