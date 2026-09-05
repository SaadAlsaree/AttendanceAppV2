using SharedKernel;

namespace Application.Abstractions.Storage;

/// <summary>
/// Whitelist validation for uploaded files. The application only ever stores employee images,
/// so anything that is not a real JPEG / PNG / WebP is rejected regardless of what the client claims.
/// </summary>
public static class ImageFileValidation
{
    public const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB — matches the frontend cap

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/webp",
    };

    public static bool IsAllowedContentType(string? contentType) =>
        !string.IsNullOrWhiteSpace(contentType) && AllowedContentTypes.Contains(contentType.Trim());

    /// <summary>
    /// Validates size, declared content type and the file signature (magic bytes).
    /// Returns the canonical content type detected from the bytes — callers must store THAT, never the client value.
    /// The stream position is reset to 0 on success.
    /// </summary>
    public static Result<string> Validate(Stream stream, string? declaredContentType, long length)
    {
        if (length <= 0 || length > MaxFileSizeBytes)
        {
            return Result.Failure<string>(FileErrors.FileSizeExceeded);
        }

        if (!IsAllowedContentType(declaredContentType))
        {
            return Result.Failure<string>(FileErrors.InvalidContentType);
        }

        byte[] header = new byte[12];
        int read = stream.Read(header, 0, header.Length);

        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        string? detected = DetectContentType(header, read);

        if (detected is null)
        {
            return Result.Failure<string>(FileErrors.InvalidContentType);
        }

        // Declared type must agree with the bytes (jpg/jpeg are the same family).
        string normalizedDeclared = declaredContentType!.Trim().Equals("image/jpg", StringComparison.OrdinalIgnoreCase)
            ? "image/jpeg"
            : declaredContentType.Trim();

        if (!normalizedDeclared.Equals(detected, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<string>(FileErrors.InvalidContentType);
        }

        return Result.Success(detected);
    }

    private static string? DetectContentType(byte[] header, int read)
    {
        if (read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            return "image/jpeg";
        }

        if (read >= 8 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47
            && header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A)
        {
            return "image/png";
        }

        if (read >= 12 && header[0] == (byte)'R' && header[1] == (byte)'I' && header[2] == (byte)'F' && header[3] == (byte)'F'
            && header[8] == (byte)'W' && header[9] == (byte)'E' && header[10] == (byte)'B' && header[11] == (byte)'P')
        {
            return "image/webp";
        }

        return null;
    }
}
