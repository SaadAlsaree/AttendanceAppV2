using Application.Abstractions.Messaging;
using Application.Features.Organizations.Shifts.GetById;
using Application.Features.Organizations.Shifts.Get;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Shifts;

internal sealed class GetById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("shifts/{id:guid}", async (
            Guid id,
            IQueryHandler<GetShiftByIdQuery, ApiResponse<ShiftResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetShiftByIdQuery { ShiftId = id };
            Result<ApiResponse<ShiftResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Shifts)
        .RequireAuthorization();
    }
}
