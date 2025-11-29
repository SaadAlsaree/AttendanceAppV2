# Get Attachments

## Description

Retrieves a paginated list of attachments with optional filtering and sorting capabilities.

## Operations

-  **Query**: `GetAttachmentsQuery`
-  **Handler**: `GetAttachmentsQueryHandler`
-  **Response**: `AttachmentResponse`

## Business Rules

-  Results are paginated for performance
-  Attachments can be filtered by organization, type, and related entity
-  Includes organization information
-  Results can be sorted by various fields
-  Supports search by attachment name or file name
-  Only shows attachments user has permission to view

## Input Parameters

-  `PageNumber` (int) - Page number for pagination
-  `PageSize` (int) - Number of items per page
-  `OrganizationId` (Guid, optional) - Filter by organization
-  `AttachmentType` (AttachmentType enum, optional) - Filter by attachment type
-  `RelatedEntityId` (Guid, optional) - Filter by related entity
-  `RelatedEntityType` (string, optional) - Filter by related entity type
-  `IsPublic` (bool, optional) - Filter by public status
-  `SearchTerm` (string, optional) - Search by attachment name or file name
-  `SortBy` (string, optional) - Field to sort by
-  `SortOrder` (SortOrder enum, optional) - Ascending or descending order

## Output

-  `PaginatedResponse<AttachmentResponse>` with attachment list and pagination metadata
-  Each attachment response includes organization information

## Related Entities

-  `Attachment` - Main entity being queried
-  `OrganizationalUnit` - Associated organization information
