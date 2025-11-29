using Domain.Common;
using Domain.Enums;
using SharedKernel;

namespace Domain.Entities.Organizations;

public sealed class Attachment : AuditableEntity<Guid>
{
    public Attachment(
        Guid employeeId,
        string fileName,
        string contentType,
        string blobPath,
        AttachmentType type,
        string? description = null)
    {
        Id = Guid.NewGuid();
        EmployeeId = employeeId;
        FileName = fileName;
        ContentType = contentType;
        BlobPath = blobPath;
        Type = type;
        Description = description;
    }

    public Guid EmployeeId { get; set; }
    public string FileName { get; private set; }
    public string ContentType { get; private set; }
    public string BlobPath { get; private set; }
    public AttachmentType Type { get; private set; }
    public string? Description { get; private set; }

    public Employee Employee { get; private set; } = null!;

    public static Result<Attachment> Create(
        Guid employeeId,
        string fileName,
        string contentType,
        string blobPath,
        AttachmentType type,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return Result.Failure<Attachment>(AttachmentErrors.FileNameRequired);
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            return Result.Failure<Attachment>(AttachmentErrors.ContentTypeRequired);
        }

        if (string.IsNullOrWhiteSpace(blobPath))
        {
            return Result.Failure<Attachment>(AttachmentErrors.BlobPathRequired);
        }

        var attachment = new Attachment(
            employeeId,
            fileName,
            contentType,
            blobPath,
            type,
            description);

        attachment.Raise(new AttachmentCreatedDomainEvent(attachment));

        return attachment;
    }

    public Result Update(
        string fileName,
        string contentType,
        string blobPath,
        AttachmentType type,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return Result.Failure(AttachmentErrors.FileNameRequired);
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            return Result.Failure(AttachmentErrors.ContentTypeRequired);
        }

        if (string.IsNullOrWhiteSpace(blobPath))
        {
            return Result.Failure(AttachmentErrors.BlobPathRequired);
        }

        FileName = fileName;
        ContentType = contentType;
        BlobPath = blobPath;
        Type = type;
        Description = description;

        Raise(new AttachmentUpdatedDomainEvent(this));

        return Result.Success();
    }
}
