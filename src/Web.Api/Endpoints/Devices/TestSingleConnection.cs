using Application.Abstractions.Data;
using Application.Models;
using Infrastructure.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace Web.Api.Endpoints.Devices;

public class TestSingleConnection : IEndpoint
{
    public sealed class Request
    {
        public Guid DeviceId { get; set; }
    }
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/devices/test-connection", async (
            [FromBody] Request request,
            [FromServices] IHikvisionService hikvisionService) =>
        {
            HikvisionResponse<DeviceStatus> result = await hikvisionService.TestConnectionAsync(request.DeviceId);

            return Results.Ok(result);
        })
        .WithTags(Tags.Devices)
        .WithName("TestSingleDeviceConnection")
        .WithOpenApi()
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
