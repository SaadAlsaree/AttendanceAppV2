using SharedKernel;

namespace Application.Abstractions.Storage;

public interface IBlobService
{
    Task<Result<Guid>> UploadAsync(Stream stream, string contentType, CancellationToken cancellationToken = default);

    Task<Result<FileResponse>> DownloadAsync(Guid fileId, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(Guid fileId, CancellationToken cancellationToken = default);
}
