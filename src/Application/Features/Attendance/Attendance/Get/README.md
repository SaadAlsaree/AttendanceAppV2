# Get Attendance Records

## Description

Retrieves a paginated list of attendance records with optional filtering, sorting, and relationship data.

## Operations

-  **Query**: `GetAttendanceQuery`
-  **Handler**: `GetAttendanceQueryHandler`
-  **Response**: `AttendanceResponse`

## Business Rules

-  Results are paginated for performance
-  Attendance records can be filtered by employee, organization, date range, and status
-  Includes employee, shift, and work location information
-  Results can be sorted by various fields
-  Supports search by employee name or number

## Input Parameters

-  `Page` (int) - Page number for pagination
-  `PageSize` (int) - Number of items per page
-  `EmployeeId` (Guid, optional) - Filter by employee
-  `OrganizationId` (Guid, optional) - Filter by organization
-  `StartDate` (DateTime, optional) - Start date for date range filter
-  `EndDate` (DateTime, optional) - End date for date range filter
-  `Status` (AttendanceStatus enum, optional) - Filter by attendance status
-  `ShiftId` (Guid, optional) - Filter by shift
-  `SearchTerm` (string, optional) - Search by employee name or number
-  `SortBy` (string, optional) - Field to sort by
-  `SortOrder` (SortOrder enum, optional) - Ascending or descending order

## Output

-  `PaginatedResponse<AttendanceResponse>` with attendance list and pagination metadata
-  Each attendance response includes employee, shift, and work location information

## Related Entities

-  `Attendance` - Main entity being queried
-  `Employee` - Associated employee information
-  `Shift` - Associated shift information
-  `WorkLocation` - Associated work location information
