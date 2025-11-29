using SharedKernel;

namespace Domain.Entities.Organizations;

internal static class AttachmentErrors
{
    public static readonly Error FileNameRequired = Error.Validation(
        "Attachment.FileNameRequired",
        "The file name is required.");

    public static readonly Error ContentTypeRequired = Error.Validation(
        "Attachment.ContentTypeRequired",
        "The content type is required.");

    public static readonly Error BlobPathRequired = Error.Validation(
        "Attachment.BlobPathRequired",
        "The blob path is required.");
}
