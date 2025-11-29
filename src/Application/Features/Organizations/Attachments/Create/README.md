# Create Attachment

## Description

Creates a new attachment record in the system for storing file metadata and references.

## Operations

-  **Command**: `CreateAttachmentCommand`
-  **Handler**: `CreateAttachmentCommandHandler`
-  **Validator**: `CreateAttachmentCommandValidator`

## Business Rules

-  Attachment name must be provided
-  File size must be within allowed limits
-  File type must be supported
-  Organization must exist
-  Attachment type must be valid (Document, Image, Certificate, etc.)
-  File path or URL must be valid
-  Content type must be specified

## Input Parameters

-  `OrganizationId` (Guid) - ID of the organization
-  `Name` (string) - Attachment name
-  `Description` (string, optional) - Attachment description
-  `FileName` (string) - Original file name
-  `FileSize` (long) - File size in bytes
-  `ContentType` (string) - MIME content type
-  `FilePath` (string) - File storage path or URL
-  `AttachmentType` (AttachmentType enum) - Type of attachment
-  `RelatedEntityId` (Guid, optional) - ID of related entity (Employee, etc.)
-  `RelatedEntityType` (string, optional) - Type of related entity
-  `IsPublic` (bool, optional) - Whether attachment is publicly accessible

## Output

-  `AttachmentResponse` with created attachment details
-  Success/Error result with appropriate messages

## Related Entities

-  `Attachment` - Main entity being created
-  `OrganizationalUnit` - Associated organization
-  `AttachmentCreatedDomainEvent` - Domain event raised on successful creation
