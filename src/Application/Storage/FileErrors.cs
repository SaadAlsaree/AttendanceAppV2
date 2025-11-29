using SharedKernel;

namespace Application.Abstractions.Storage;

public static class FileErrors
{
    public static Error NotFound(Guid fileId) => Error.NotFound(
        "File.NotFound",
        $"The file with ID {fileId} was not found");

    public static readonly Error UploadFailed = Error.Problem(
        "File.UploadFailed",
        "Failed to upload the file");

    public static readonly Error InvalidContentType = Error.Validation(
        "File.InvalidContentType",
        "The file content type is not supported");

    public static readonly Error FileSizeExceeded = Error.Validation(
        "File.FileSizeExceeded",
        "The file size exceeds the maximum allowed size");
}
