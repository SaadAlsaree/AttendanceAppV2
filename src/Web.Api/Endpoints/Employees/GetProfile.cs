using Application.Abstractions.Messaging;
using Application.Features.Organizations.Employees.Profile;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Employees;

internal sealed class GetProfile : IEndpoint
{
    public sealed class Request
    {
        public Guid EmployeeId { get; set; }
    }
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("employees/profile", async (
            [FromBody] Request request,
            IQueryHandler<GetProfileQuery, ProfileResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetProfileQuery(request.EmployeeId);

            Result<ProfileResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Employees)
        .RequireAuthorization();
    }
}
