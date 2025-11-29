# Update Attachment

## Description

Updates an existing attachment's metadata and properties.

## Operations

-  **Command**: `UpdateAttachmentCommand`
-  **Handler**: `UpdateAttachmentCommandHandler`
-  **Validator**: `UpdateAttachmentCommandValidator`

## Business Rules

-  Attachment must exist in the system
-  User must have permission to update the attachment
-  File path cannot be changed (file replacement requires new attachment)
-  Name and description can be updated
-  Attachment type can be modified
-  Public status can be changed
-  Related entity associations can be updated

## Input Parameters

-  `AttachmentId` (Guid) - Unique identifier of the attachment to update
-  `Name` (string, optional) - Updated attachment name
-  `Description` (string, optional) - Updated attachment description
-  `AttachmentType` (AttachmentType enum, optional) - Updated attachment type
-  `RelatedEntityId` (Guid, optional) - Updated related entity ID
-  `RelatedEntityType` (string, optional) - Updated related entity type
-  `IsPublic` (bool, optional) - Updated public status

## Output

-  `AttachmentResponse` with updated attachment details
-  Success/Error result with appropriate messages

## Related Entities

-  `Attachment` - Main entity being updated
-  `OrganizationalUnit` - Associated organization
-  `AttachmentUpdatedDomainEvent` - Domain event raised on successful update
