using Application.Abstractions.Messaging;
using Application.Features.Organizations.Employees.Update;
using Infrastructure.Authentication;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Employees;

internal sealed class Update : IEndpoint
{
    public sealed class Request
    {
        public Guid EmployeeId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string SecondName { get; set; } = string.Empty;
        public string ThirdName { get; set; } = string.Empty;
        public string FourthName { get; set; } = string.Empty;
        public string FamilyName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string RFID { get; set; } = string.Empty;
        public string EmpId { get; set; } = string.Empty;
        public Guid OrganizationalUnitId { get; set; }
        public Guid? ManagerId { get; set; }
        public bool IsManager { get; set; }

    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("employees/{id:guid}", async (
            Guid id,
           [FromBody] Request request,
            ICommandHandler<UpdateEmployeeCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateEmployeeCommand
            {
                Id = id,
                FirstName = request.FirstName,
                SecondName = request.SecondName,
                ThirdName = request.ThirdName,
                FourthName = request.FourthName,
                FamilyName = request.FamilyName,
                RFID = request.RFID,
                EmpId = request.EmpId,
                ManagerId = request.ManagerId,
                OrganizationalUnitId = request.OrganizationalUnitId,
                IsManager = request.IsManager,

            };

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Employees)
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
