using Application.Abstractions.Data;
using Application.Models;
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
        .WithOpenApi();
    }
}
