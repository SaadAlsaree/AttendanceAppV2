using Application.Abstractions.Messaging;
using Application.Organizations.Holidays.GetById;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Holidays;

internal sealed class GetById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("holidays/{id:guid}", async (
            Guid id,
            IQueryHandler<GetHolidayByIdQuery, ApiResponse<HolidayResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetHolidayByIdQuery(id);
            Result<ApiResponse<HolidayResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Holidays);
        //.RequireAuthorization();
    }
}
