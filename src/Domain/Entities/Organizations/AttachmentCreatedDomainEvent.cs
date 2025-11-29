using SharedKernel;

namespace Domain.Entities.Organizations;

public sealed record AttachmentCreatedDomainEvent(Attachment Attachment) : IDomainEvent;
