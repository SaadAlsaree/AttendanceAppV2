# Get My Attendance Schedules

## Description

Retrieves a paginated list of attendance schedules for the currently authenticated user/employee.

## Operations

-  **Query**: `GetMySchedulesQuery`
-  **Handler**: `GetMySchedulesQueryHandler`
-  **Response**: `AttendanceScheduleResponse`

## Business Rules

-  Results are paginated for performance
-  Only returns schedules for the currently authenticated user
-  Schedules can be filtered by schedule type and active status
-  Results can be sorted by various fields
-  Includes shift information
-  Only active schedules are returned by default

## Input Parameters

-  `PageNumber` (int) - Page number for pagination
-  `PageSize` (int) - Number of items per page
-  `ScheduleType` (ScheduleType enum, optional) - Filter by schedule type
-  `IsActive` (bool, optional) - Filter by active status
-  `SortBy` (string, optional) - Field to sort by
-  `SortOrder` (string, optional) - Ascending or descending order

## Output

-  `PaginatedResponse<AttendanceScheduleResponse>` with schedule list and pagination metadata
-  Each schedule response includes shift information

## Related Entities

-  `AttendanceSchedule` - Main entity being queried
-  `Shift` - Associated shift information
-  `Employee` - Current user's employee record
