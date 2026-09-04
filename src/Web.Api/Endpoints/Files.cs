using Application.Abstractions.Storage;
using Infrastructure.Authentication;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints;

public class Files : IEndpoint
{
    private static readonly string[] UploadRoles = ["Admin", "SuperAdmin", "Manager", "OrgSupervisor"];
    private static readonly string[] DeleteRoles = ["Admin", "SuperAdmin"];

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        // Upload: authenticated staff only; image whitelist + magic bytes + size cap enforced in IBlobService.
        app.MapPost("files", async (IFormFile file, IBlobService blobService, CancellationToken cancellationToken) =>
        {
            using Stream stream = file.OpenReadStream();

            Result<Guid> result = await blobService.UploadAsync(stream, file.ContentType, cancellationToken);

            return result.Match(
                fileId => Results.Ok(fileId),
                CustomResults.Problem);
        })
        .WithTags("Files")
        .DisableAntiforgery()
        .RequireAuthorization(policy => policy.RequireAssertion(context => HasRole(context.User, UploadRoles)))
        .RequireRateLimiting("fixed");

        // Download: intentionally anonymous — employee face images are rendered by plain <img src> tags
        // that carry no bearer token. Ids are unguessable GUIDs. Only whitelisted image types are ever served,
        // with nosniff + a lock-down CSP so nothing can execute in the browser even if a bad blob exists.
        app.MapGet("files/{fileId}", async (Guid fileId, IBlobService blobService, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            Result<FileResponse> result = await blobService.DownloadAsync(fileId, cancellationToken);

            if (result.IsFailure)
            {
                return CustomResults.Problem(result);
            }

            if (!ImageFileValidation.IsAllowedContentType(result.Value.ContentType))
            {
                return CustomResults.Problem(Result.Failure(FileErrors.NotFound(fileId)));
            }

            httpContext.Response.Headers["X-Content-Type-Options"] = "nosniff";
            httpContext.Response.Headers["Content-Security-Policy"] = "default-src 'none'; sandbox";
            httpContext.Response.Headers["Cache-Control"] = "private, max-age=3600";

            return Results.File(result.Value.Stream, result.Value.ContentType);
        })
        .WithTags("Files")
        .RequireRateLimiting("fixed");

        app.MapDelete("files/{fileId}", async (Guid fileId, IBlobService blobService, CancellationToken cancellationToken) =>
        {
            Result result = await blobService.DeleteAsync(fileId, cancellationToken);

            return result.Match(
                () => Results.NoContent(),
                CustomResults.Problem);
        })
        .WithTags("Files")
        .RequireAuthorization(policy => policy.RequireAssertion(context => HasRole(context.User, DeleteRoles)))
        .RequireRateLimiting("fixed");
    }

    private static bool HasRole(System.Security.Claims.ClaimsPrincipal user, string[] allowedRoles)
    {
        if (user.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        string? userRole = user.GetRole();

        return !string.IsNullOrWhiteSpace(userRole)
            && allowedRoles.Contains(userRole, StringComparer.OrdinalIgnoreCase);
    }
}
