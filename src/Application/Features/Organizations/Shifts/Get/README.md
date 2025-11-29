# Get Shifts

## Description

Retrieves a paginated list of shifts with optional filtering and sorting capabilities.

## Operations

-  **Query**: `GetShiftsQuery`
-  **Handler**: `GetShiftsQueryHandler`
-  **Response**: `ShiftResponse`

## Business Rules

-  Results are paginated for performance
-  Shifts can be filtered by organization, type, and active status
-  Includes organization information
-  Results can be sorted by various fields
-  Supports search by shift name
-  Only active shifts are returned by default

## Input Parameters

-  `Page` (int) - Page number for pagination
-  `PageSize` (int) - Number of items per page
-  `OrganizationId` (Guid, optional) - Filter by organization
-  `ShiftType` (ShiftType enum, optional) - Filter by shift type
-  `IsActive` (bool, optional) - Filter by active status
-  `SearchTerm` (string, optional) - Search by shift name
-  `SortBy` (string, optional) - Field to sort by
-  `SortOrder` (SortOrder enum, optional) - Ascending or descending order

## Output

-  `PaginatedResponse<ShiftResponse>` with shift list and pagination metadata
-  Each shift response includes organization information

## Related Entities

-  `Shift` - Main entity being queried
-  `OrganizationalUnit` - Associated organization information
