using Application.Abstractions.Messaging;
using Application.Organizations.OrganizationalUnits.Update;
using Infrastructure.Authentication;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.OrganizationalUnits;

internal sealed class Update : IEndpoint
{
    public sealed class Request
    {
        public Guid OrganizationalUnitId { get; set; }
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
        app.MapPut("organizational-units/{id:guid}", async (
            Guid id,
            Request request,
            ICommandHandler<UpdateOrganizationalUnitCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateOrganizationalUnitCommand
            {
                OrganizationalUnitId = id,
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

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.OrganizationalUnits)
         .RequireAuthorization(policy => policy
     .RequireAssertion(context =>
     {
         if (context.User.Identity?.IsAuthenticated != true)
         {
             return false;
         }

         // الحصول على Role من JWT Token Claims
         string? userRole = context.User.GetRole();

         if (string.IsNullOrWhiteSpace(userRole))
         {
             return false;
         }

         // OR logic: إذا كان لديه أي Role من الأدوار المطلوبة
         string[] allowedRoles = ["Admin", "SuperAdmin"];
         return allowedRoles.Contains(userRole, StringComparer.OrdinalIgnoreCase);
     }));
    }
}
