using Application.Abstractions.Messaging;
using Application.Features.Organizations.Employees.GetById;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Employees;

internal sealed class GetById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("employees/{id:guid}", async (
            Guid id,
            IQueryHandler<GetEmployeeByIdQuery, ApiResponse<GetEmployeeByIdVm>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetEmployeeByIdQuery { Id = id };

            Result<ApiResponse<GetEmployeeByIdVm>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Employees);
        //.RequireAuthorization();
    }
}
