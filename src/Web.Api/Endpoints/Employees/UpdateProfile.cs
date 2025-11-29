using Application.Abstractions.Messaging;
using Application.Features.Organizations.Employees.Profile;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Employees;

internal sealed class UpdateProfile : IEndpoint
{
    public sealed class Request
    {
        public Guid EmployeeId { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? ProfileImage { get; set; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("employees/profile", async (
            [FromBody] Request request,
            ICommandHandler<UpdateProfileCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateProfileCommand(
                request.EmployeeId,
                request.ProfileImage);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Employees)
        .RequireAuthorization();
    }
}
