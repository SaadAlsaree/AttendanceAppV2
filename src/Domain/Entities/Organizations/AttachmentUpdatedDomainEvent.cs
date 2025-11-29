using SharedKernel;

namespace Domain.Entities.Organizations;

public sealed record AttachmentUpdatedDomainEvent(Attachment Attachment) : IDomainEvent;
