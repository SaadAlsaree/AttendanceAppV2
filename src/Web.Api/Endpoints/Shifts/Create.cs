using System.Globalization;
using Application.Abstractions.Messaging;
using Application.Features.Organizations.Shifts.Create;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Shifts;

internal sealed class Create : IEndpoint
{
    public sealed class Request
    {
        public string Name { get; set; } = string.Empty;
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string ShiftType { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string? Description { get; set; }
        public int? GracePeriodMinutes { get; set; }
        public int? MaxLateMinutes { get; set; }
        public bool AllowEarlyCheckIn { get; set; }
        public bool AllowLateCheckOut { get; set; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("shifts", async (
            [FromBody] Request request,
            ICommandHandler<CreateShiftCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var start = TimeOnly.ParseExact(request.StartTime, "HH:mm:ss", CultureInfo.InvariantCulture);
            var end = TimeOnly.ParseExact(request.EndTime, "HH:mm:ss", CultureInfo.InvariantCulture);
            var command = new CreateShiftCommand
            {
                Name = request.Name,
                StartTime = start,
                EndTime = end,
                ShiftType = Enum.Parse<ShiftType>(request.ShiftType),
                IsActive = request.IsActive,
                Description = request.Description,
                GracePeriodMinutes = request.GracePeriodMinutes,
                MaxLateMinutes = request.MaxLateMinutes,
                AllowEarlyCheckIn = request.AllowEarlyCheckIn,
                AllowLateCheckOut = request.AllowLateCheckOut
            };

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Shifts)
        .RequireAuthorization();
    }
}
