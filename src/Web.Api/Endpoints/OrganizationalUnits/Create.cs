using Application.Abstractions.Messaging;
using Application.Organizations.OrganizationalUnits.Create;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.OrganizationalUnits;

internal sealed class Create : IEndpoint
{
    public sealed class Request
    {
        public string UnitName { get; set; } = string.Empty;
        public string UnitCode { get; set; } = string.Empty;
        public string? UnitDescription { get; set; }
        public Guid? ParentUnitId { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? PostalCode { get; set; }
        public string? UnitLogo { get; set; }
        public int? UnitLevel { get; set; }
        public Guid? ManagerId { get; set; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("organizational-units", async (
            Request request,
            ICommandHandler<CreateOrganizationalUnitCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateOrganizationalUnitCommand
            {
                UnitName = request.UnitName,
                UnitCode = request.UnitCode,
                UnitDescription = request.UnitDescription,
                ParentUnitId = request.ParentUnitId,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                Address = request.Address,
                PostalCode = request.PostalCode,
                UnitLogo = request.UnitLogo,
                UnitLevel = request.UnitLevel,
                ManagerId = request.ManagerId
            };

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.OrganizationalUnits);
        //.RequireAuthorization();
    }
}
