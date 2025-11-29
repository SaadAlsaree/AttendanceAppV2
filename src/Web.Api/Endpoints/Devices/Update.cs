using Application.Abstractions.Messaging;
using Application.Devices.Update;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Devices;

internal sealed class Update : IEndpoint
{
    public sealed class Request
    {
        public Guid Id { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? Location { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public string? DeviceId { get; set; }
        public string? IsupKey { get; set; }
        public string? Port { get; set; }
        public string? Protocol { get; set; }
        public string? DeviceModel { get; set; }
        public string? SerialNumber { get; set; }
        public string? MacAddress { get; set; }
        public string? FirmwareVersion { get; set; }
        public string? Department { get; set; }
        public string? Features { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastConnected { get; set; }
        public Guid? OrganizationId { get; set; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("devices/{id:guid}", async (
            Guid id,
            [FromBody] Request request,
            ICommandHandler<UpdateDeviceCommand, bool> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateDeviceCommand
            {
                Id = id,
                Username = request.Username,
                Password = request.Password,
                Location = request.Location,
                IpAddress = request.IpAddress,
                DeviceId = request.DeviceId,
                IsupKey = request.IsupKey,
                Port = request.Port,
                Protocol = request.Protocol,
                DeviceModel = request.DeviceModel,
                SerialNumber = request.SerialNumber,
                MacAddress = request.MacAddress,
                FirmwareVersion = request.FirmwareVersion,
                Department = request.Department,
                Features = request.Features,
                IsActive = request.IsActive,
                LastConnected = request.LastConnected,
                OrganizationId = request.OrganizationId,
            };

            Result<bool> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Devices)
        .RequireAuthorization();
    }
}
