using Application.Abstractions.Messaging;
using Application.Organizations.Holidays.Create;
using Infrastructure.Authentication;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Holidays;

internal sealed class Create : IEndpoint
{
    public sealed class Request
    {
        public Guid OrganizationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateOnly Date { get; set; }
        public bool IsRecurring { get; set; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("holidays", async (
            [FromBody] Request request,
            ICommandHandler<CreateHolidayCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateHolidayCommand
            {
                OrganizationId = request.OrganizationId,
                Name = request.Name,
                Date = request.Date,
                IsRecurring = request.IsRecurring
            };

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Holidays)
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
         string[] allowedRoles = ["Admin", "Employee", "Manager", "SuperAdmin"];
         return allowedRoles.Contains(userRole, StringComparer.OrdinalIgnoreCase);
     }));
        //.RequireAuthorization();
    }
}
