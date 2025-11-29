# Get Attendance Breaks

## Description

Retrieves a paginated list of attendance breaks with optional filtering and sorting capabilities.

## Operations

-  **Query**: `GetAttendanceBreaksQuery`
-  **Handler**: `GetAttendanceBreaksQueryHandler`
-  **Response**: `AttendanceBreakResponse`

## Business Rules

-  Results are paginated for performance
-  Breaks can be filtered by employee, attendance record, date range, and break type
-  Includes employee and attendance information
-  Results can be sorted by various fields
-  Supports search by employee name or break type

## Input Parameters

-  `Page` (int) - Page number for pagination
-  `PageSize` (int) - Number of items per page
-  `EmployeeId` (Guid, optional) - Filter by employee
-  `AttendanceId` (Guid, optional) - Filter by attendance record
-  `StartDate` (DateTime, optional) - Start date for date range filter
-  `EndDate` (DateTime, optional) - End date for date range filter
-  `BreakType` (BreakType enum, optional) - Filter by break type
-  `SearchTerm` (string, optional) - Search by employee name
-  `SortBy` (string, optional) - Field to sort by
-  `SortOrder` (SortOrder enum, optional) - Ascending or descending order

## Output

-  `PaginatedResponse<AttendanceBreakResponse>` with break list and pagination metadata
-  Each break response includes employee and attendance information

## Related Entities

-  `AttendanceBreak` - Main entity being queried
-  `Employee` - Associated employee information
-  `Attendance` - Associated attendance record information
