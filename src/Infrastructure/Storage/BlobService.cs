using Application.Abstractions.Storage;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using SharedKernel;

namespace Infrastructure.Storage;

internal sealed class BlobService(BlobServiceClient blobServiceClient) : IBlobService
{
    private const string ContainerName = "files";

    private async Task<BlobContainerClient> EnsureContainerExistsAsync(CancellationToken cancellationToken = default)
    {
        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(ContainerName);

        await containerClient.CreateIfNotExistsAsync(
            PublicAccessType.None,
            cancellationToken: cancellationToken);

        return containerClient;
    }

    public async Task<Result<Guid>> UploadAsync(Stream stream, string contentType, CancellationToken cancellationToken = default)
    {
        // Whitelist + magic-byte check. The stored content type is the DETECTED one, never the client's.
        long length = stream.CanSeek ? stream.Length : ImageFileValidation.MaxFileSizeBytes + 1;
        Result<string> validation = ImageFileValidation.Validate(stream, contentType, length);
        if (validation.IsFailure)
        {
            return Result.Failure<Guid>(validation.Error);
        }

        try
        {
            BlobContainerClient containerClient = await EnsureContainerExistsAsync(cancellationToken);

            var fileId = Guid.NewGuid();
            BlobClient blobClient = containerClient.GetBlobClient(fileId.ToString());

            await blobClient.UploadAsync(
                stream,
                new BlobHttpHeaders { ContentType = validation.Value },
                cancellationToken: cancellationToken);

            return Result.Success(fileId);
        }
        catch (Exception)
        {
            return Result.Failure<Guid>(FileErrors.UploadFailed);
        }
    }

    public async Task<Result<FileResponse>> DownloadAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        try
        {
            BlobContainerClient containerClient = await EnsureContainerExistsAsync(cancellationToken);

            BlobClient blobClient = containerClient.GetBlobClient(fileId.ToString());

            if (!await blobClient.ExistsAsync(cancellationToken))
            {
                return Result.Failure<FileResponse>(FileErrors.NotFound(fileId));
            }

            Response<BlobDownloadResult> response = await blobClient.DownloadContentAsync(cancellationToken: cancellationToken);

            return Result.Success(new FileResponse(response.Value.Content.ToStream(), response.Value.Details.ContentType));
        }
        catch (Exception)
        {
            return Result.Failure<FileResponse>(FileErrors.NotFound(fileId));
        }
    }

    public async Task<Result> DeleteAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        try
        {
            BlobContainerClient containerClient = await EnsureContainerExistsAsync(cancellationToken);

            BlobClient blobClient = containerClient.GetBlobClient(fileId.ToString());

            await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);

            return Result.Success();
        }
        catch (Exception)
        {
            return Result.Failure(FileErrors.NotFound(fileId));
        }
    }
}
