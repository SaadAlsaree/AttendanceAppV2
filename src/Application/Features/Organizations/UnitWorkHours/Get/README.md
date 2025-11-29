# Get Unit Work Hours

## Description

Retrieves a paginated list of unit work hours configurations with optional filtering and sorting capabilities.

## Operations

-  **Query**: `GetUnitWorkHoursQuery`
-  **Handler**: `GetUnitWorkHoursQueryHandler`
-  **Response**: `UnitWorkHoursResponse`

## Business Rules

-  Results are paginated for performance
-  Work hours can be filtered by organizational unit and day of week
-  Includes organizational unit information
-  Results can be sorted by various fields
-  Supports search by unit name

## Input Parameters

-  `PageNumber` (int) - Page number for pagination
-  `PageSize` (int) - Number of items per page
-  `OrganizationalUnitId` (Guid, optional) - Filter by organizational unit
-  `DayOfWeek` (DayOfWeek enum, optional) - Filter by day of week
-  `IsWorkingDay` (bool, optional) - Filter by working day status
-  `SearchTerm` (string, optional) - Search by unit name
-  `SortBy` (string, optional) - Field to sort by
-  `SortOrder` (SortOrder enum, optional) - Ascending or descending order

## Output

-  `PaginatedResponse<UnitWorkHoursResponse>` with work hours list and pagination metadata
-  Each work hours response includes organizational unit information

## Related Entities

-  `UnitWorkHours` - Main entity being queried
-  `OrganizationalUnit` - Associated organizational unit information
