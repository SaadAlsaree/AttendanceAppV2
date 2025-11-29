using Application.Abstractions.Messaging;
using Application.Organizations.Holidays.Delete;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Holidays;

internal sealed class Delete : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("holidays/{id:guid}", async (
            Guid id,
            ICommandHandler<DeleteHolidayCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new DeleteHolidayCommand { HolidayId = id };
            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Holidays);
        //.RequireAuthorization();
    }
}
