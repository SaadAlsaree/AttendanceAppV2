# Get Organizational Units

## Description

Retrieves a paginated list of organizational units with hierarchical structure and relationship data.

## Operations

-  **Query**: `GetOrganizationalUnitsQuery`
-  **Handler**: `GetOrganizationalUnitsQueryHandler`
-  **Response**: `OrganizationalUnitResponse`

## Business Rules

-  Results are paginated for performance
-  Units can be filtered by organization, parent unit, and active status
-  Includes hierarchical structure information
-  Results can be sorted by various fields
-  Supports search by unit name or code
-  Includes manager and employee count information

## Input Parameters

-  `PageNumber` (int) - Page number for pagination
-  `PageSize` (int) - Number of items per page
-  `OrganizationId` (Guid, optional) - Filter by organization
-  `ParentUnitId` (Guid, optional) - Filter by parent unit
-  `ManagerId` (Guid, optional) - Filter by manager
-  `IsActive` (bool, optional) - Filter by active status
-  `SearchTerm` (string, optional) - Search by unit name or code
-  `SortBy` (string, optional) - Field to sort by
-  `SortOrder` (SortOrder enum, optional) - Ascending or descending order

## Output

-  `PaginatedResponse<OrganizationalUnitResponse>` with unit list and pagination metadata
-  Each unit response includes hierarchical and manager information

## Related Entities

-  `OrganizationalUnit` - Main entity being queried
-  `Employee` - Manager information
-  `Employee` - Employee count information
