using Application.Abstractions.Messaging;
using Application.Abstractions.Storage;
using Application.Features.Organizations.Employees.Registration;
using Infrastructure.Authentication;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Auth;

internal sealed class Register : IEndpoint
{
    public sealed class Request
    {
        // Employee information
        [FromForm] public string EmpId { get; set; } = string.Empty;
        [FromForm] public string FirstName { get; set; } = string.Empty;
        [FromForm] public string SecondName { get; set; } = string.Empty;
        [FromForm] public string ThirdName { get; set; } = string.Empty;
        [FromForm] public string FourthName { get; set; } = string.Empty;
        [FromForm] public string FamilyName { get; set; } = string.Empty;
        [FromForm] public string RFID { get; set; } = string.Empty;
        [FromForm] public Guid OrganizationalUnitId { get; set; }

        // Using string for form input to handle empty values properly
        [FromForm] public string ManagerIdString { get; set; } = string.Empty;

        // Property to convert string to Guid? when needed
        public Guid? ManagerId => !string.IsNullOrWhiteSpace(ManagerIdString) ? Guid.Parse(ManagerIdString) : null;

        [FromForm] public bool IsManager { get; set; }

        // File inputs
        [FromForm] public IFormFile? FaceImage { get; set; } = null!;

    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("auth/register", async (
            [FromForm] Request request,
            ICommandHandler<RegisterEmployeeCommand, Guid> handler,
            IBlobService blobService,
            IDateTimeProvider dateTimeProvider,
            CancellationToken cancellationToken) =>
        {
            string faceImageUrl = string.Empty;

            // Handle face image upload if provided
            if (request.FaceImage is not null)
            {
                using Stream stream = request.FaceImage.OpenReadStream();

                Result<Guid> faceImageResult = await blobService.UploadAsync(stream, request.FaceImage.ContentType, cancellationToken);

                if (faceImageResult.IsFailure)
                {
                    return Results.BadRequest(faceImageResult.Error);
                }

                faceImageUrl = $"http://localhost:5000/files/{faceImageResult.Value}";
            }

            var command = new RegisterEmployeeCommand
            {
                // Employee information
                EmpId = request.EmpId,
                FirstName = request.FirstName,
                SecondName = request.SecondName,
                ThirdName = request.ThirdName,
                FourthName = request.FourthName,
                FamilyName = request.FamilyName,
                RFID = request.RFID,
                OrganizationalUnitId = request.OrganizationalUnitId,
                ManagerId = request.ManagerId,
                IsManager = request.IsManager,
                FaceImageUrl = faceImageUrl,
            };

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(
                id => Results.Created($"employees/{id}", id),
                CustomResults.Problem);
        })
        .WithTags(Tags.Auth)
        .DisableAntiforgery()
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
     }))
     .RequireRateLimiting("per-user");
    }
}
