using Application.Abstractions.Messaging;
using Application.Organizations.Holidays.Update;
using Infrastructure.Authentication;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Holidays;

internal sealed class Update : IEndpoint
{
    public sealed class Request
    {
        public string? Name { get; set; }
        public DateOnly? Date { get; set; }
        public bool? IsRecurring { get; set; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("holidays/{id:guid}", async (
            Guid id,
            [FromBody] Request request,
            ICommandHandler<UpdateHolidayCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateHolidayCommand
            {
                HolidayId = id,
                Name = request.Name,
                Date = request.Date,
                IsRecurring = request.IsRecurring
            };

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Holidays).RequireAuthorization(policy => policy
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
    }
}
