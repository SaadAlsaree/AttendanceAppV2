using Application.Abstractions.Messaging;
using Application.Features.Organizations.Employees.Get;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Employees;

internal sealed class Get : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("employees", async (
            [AsParameters] GetEmployeesQuery query,
            IQueryHandler<GetEmployeesQuery, PaginatedResponse<EmployeeResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            Result<PaginatedResponse<EmployeeResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Employees)
        .RequireAuthorization();
    }
}
