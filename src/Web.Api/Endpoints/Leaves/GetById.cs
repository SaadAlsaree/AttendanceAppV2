using Application.Abstractions.Messaging;
using Application.Attendance.Leaves.GetById;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Leaves;

internal sealed class GetById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("leaves/{id}", async (
            Guid id,
            IQueryHandler<GetLeaveByIdQuery, LeaveResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetLeaveByIdQuery
            {
                LeaveId = id
            };

            Result<LeaveResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Leaves)
        .RequireAuthorization();
    }
}
