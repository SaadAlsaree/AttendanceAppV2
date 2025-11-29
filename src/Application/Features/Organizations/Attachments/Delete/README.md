# Delete Attachment

## Description

Deletes an attachment from the system, including the physical file and metadata.

## Operations

-  **Command**: `DeleteAttachmentCommand`
-  **Handler**: `DeleteAttachmentCommandHandler`
-  **Validator**: `DeleteAttachmentCommandValidator`

## Business Rules

-  Attachment must exist in the system
-  User must have permission to delete the attachment
-  Physical file is deleted from storage
-  Metadata is removed from database
-  System may perform soft delete instead of hard delete
-  Deletion is logged for audit purposes
-  Related entity associations are cleaned up

## Input Parameters

-  `AttachmentId` (Guid) - Unique identifier of the attachment to delete

## Output

-  Success/Error result with appropriate messages
-  Confirmation of deletion operation

## Related Entities

-  `Attachment` - Main entity being deleted
-  `IBlobService` - Storage service for file deletion
