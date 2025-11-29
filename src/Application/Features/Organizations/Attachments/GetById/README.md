# Get Attachment By ID

## Description

Retrieves a specific attachment by its unique identifier with complete details and relationships.

## Operations

-  **Query**: `GetAttachmentByIdQuery`
-  **Handler**: `GetAttachmentByIdQueryHandler`
-  **Response**: `AttachmentResponse`

## Business Rules

-  Attachment must exist in the system
-  Returns complete attachment information including relationships
-  Includes organization details
-  User must have permission to view the attachment
-  File metadata is included in response

## Input Parameters

-  `AttachmentId` (Guid) - Unique identifier of the attachment to retrieve

## Output

-  `AttachmentResponse` with complete attachment details and relationships
-  Error result if attachment is not found

## Related Entities

-  `Attachment` - Main entity being queried
-  `OrganizationalUnit` - Associated organization information
