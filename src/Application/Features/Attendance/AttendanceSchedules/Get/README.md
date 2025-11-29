# Get Attendance Schedules

## Description

Retrieves a paginated list of attendance schedules with optional filtering and sorting capabilities.

## Operations

-  **Query**: `GetAttendanceSchedulesQuery`
-  **Handler**: `GetAttendanceSchedulesQueryHandler`
-  **Response**: `AttendanceScheduleResponse`

## Business Rules

-  Results are paginated for performance
-  Schedules can be filtered by organization, schedule type, and active status
-  Includes organization information
-  Results can be sorted by various fields
-  Supports search by schedule name
-  Only active schedules are returned by default

## Input Parameters

-  `PageNumber` (int) - Page number for pagination
-  `PageSize` (int) - Number of items per page
-  `OrganizationId` (Guid, optional) - Filter by organization
-  `ScheduleType` (ScheduleType enum, optional) - Filter by schedule type
-  `IsActive` (bool, optional) - Filter by active status
-  `SearchTerm` (string, optional) - Search by schedule name
-  `SortBy` (string, optional) - Field to sort by
-  `SortOrder` (SortOrder enum, optional) - Ascending or descending order

## Output

-  `PaginatedResponse<AttendanceScheduleResponse>` with schedule list and pagination metadata
-  Each schedule response includes organization information

## Related Entities

-  `AttendanceSchedule` - Main entity being queried
-  `OrganizationalUnit` - Associated organization information
