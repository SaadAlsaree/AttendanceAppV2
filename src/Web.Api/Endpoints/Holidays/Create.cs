using Application.Abstractions.Messaging;
using Application.Organizations.Holidays.Create;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Holidays;

internal sealed class Create : IEndpoint
{
    public sealed class Request
    {
        public Guid OrganizationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateOnly Date { get; set; }
        public bool IsRecurring { get; set; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("holidays", async (
            [FromBody] Request request,
            ICommandHandler<CreateHolidayCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateHolidayCommand
            {
                OrganizationId = request.OrganizationId,
                Name = request.Name,
                Date = request.Date,
                IsRecurring = request.IsRecurring
            };

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Holidays);
        //.RequireAuthorization();
    }
}
