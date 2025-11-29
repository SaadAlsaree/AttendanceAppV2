using Application.Abstractions.Data;
using Application.Models;
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
        .WithOpenApi();
    }
}
