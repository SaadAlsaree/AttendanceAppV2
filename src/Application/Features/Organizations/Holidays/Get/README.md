# Get Holidays

## Description

Retrieves a paginated list of holidays with optional filtering and sorting capabilities.

## Operations

-  **Query**: `GetHolidaysQuery`
-  **Handler**: `GetHolidaysQueryHandler`
-  **Response**: `HolidayResponse`

## Business Rules

-  Results are paginated for performance
-  Holidays can be filtered by organization, date range, and type
-  Includes organization information
-  Results can be sorted by various fields
-  Supports search by holiday name
-  Can filter by recurring and paid holidays

## Input Parameters

-  `PageNumber` (int) - Page number for pagination
-  `PageSize` (int) - Number of items per page
-  `OrganizationId` (Guid, optional) - Filter by organization
-  `StartDate` (DateTime, optional) - Start date for date range filter
-  `EndDate` (DateTime, optional) - End date for date range filter
-  `IsRecurring` (bool, optional) - Filter by recurring holidays
-  `IsPaid` (bool, optional) - Filter by paid holidays
-  `SearchTerm` (string, optional) - Search by holiday name
-  `SortBy` (string, optional) - Field to sort by
-  `SortOrder` (SortOrder enum, optional) - Ascending or descending order

## Output

-  `PaginatedResponse<HolidayResponse>` with holiday list and pagination metadata
-  Each holiday response includes organization information

## Related Entities

-  `Holiday` - Main entity being queried
-  `OrganizationalUnit` - Associated organization information
