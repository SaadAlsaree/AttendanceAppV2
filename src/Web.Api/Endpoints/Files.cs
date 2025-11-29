using Application.Abstractions.Storage;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints;

public class Files : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("files", async (IFormFile file, IBlobService blobService) =>
        {
            using Stream stream = file.OpenReadStream();

            Result<Guid> result = await blobService.UploadAsync(stream, file.ContentType);

            return result.Match(
                fileId => Results.Ok(fileId),
                CustomResults.Problem);
        })
        .WithTags("Files")
        .DisableAntiforgery();

        app.MapGet("files/{fileId}", async (Guid fileId, IBlobService blobService) =>
        {
            Result<FileResponse> result = await blobService.DownloadAsync(fileId);

            return result.Match(
                fileResponse => Results.File(fileResponse.Stream, fileResponse.ContentType),
                CustomResults.Problem);
        })
        .WithTags("Files");

        app.MapDelete("files/{fileId}", async (Guid fileId, IBlobService blobService) =>
        {
            Result result = await blobService.DeleteAsync(fileId);

            return result.Match(
                () => Results.NoContent(),
                CustomResults.Problem);
        })
        .WithTags("Files");
    }
}
