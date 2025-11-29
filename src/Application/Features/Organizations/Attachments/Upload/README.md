# Upload Attachment

## Description

Uploads a file and creates an attachment record with metadata and storage information.

## Operations

-  **Command**: `UploadAttachmentCommand`
-  **Handler**: `UploadAttachmentCommandHandler`
-  **Validator**: `UploadAttachmentCommandValidator`

## Business Rules

-  File must be provided and valid
-  File size must be within allowed limits
-  File type must be supported
-  Organization must exist
-  File is uploaded to secure storage
-  Attachment metadata is created
-  File path is generated and stored
-  Content type is automatically detected

## Input Parameters

-  `OrganizationId` (Guid) - ID of the organization
-  `File` (IFormFile) - File to upload
-  `Name` (string) - Attachment name
-  `Description` (string, optional) - Attachment description
-  `AttachmentType` (AttachmentType enum) - Type of attachment
-  `RelatedEntityId` (Guid, optional) - ID of related entity
-  `RelatedEntityType` (string, optional) - Type of related entity
-  `IsPublic` (bool, optional) - Whether attachment is publicly accessible

## Output

-  `AttachmentResponse` with uploaded attachment details
-  Success/Error result with appropriate messages

## Related Entities

-  `Attachment` - Main entity being created
-  `OrganizationalUnit` - Associated organization
-  `IBlobService` - Storage service for file upload
-  `AttachmentCreatedDomainEvent` - Domain event raised on successful upload
