using Application.Abstractions.Data;
using Application.Models;
using Infrastructure.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace Web.Api.Endpoints.Devices;

public class TestAllConnections : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/devices/test-all-connection", async (
            [FromServices] IHikvisionService hikvisionService) =>
        {
            HikvisionResponse<List<DeviceStatus>> result = await hikvisionService.TestAllDevicesConnectionAsync();

            return Results.Ok(result);
        })
        .WithTags(Tags.Devices)
        .WithName("TestAllDevicesConnection")
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
        .WithOpenApi();
    }
}
