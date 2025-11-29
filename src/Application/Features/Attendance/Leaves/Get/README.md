# Get Leave Requests

## Description

Retrieves a paginated list of leave requests with optional filtering, sorting, and relationship data.

## Operations

-  **Query**: `GetLeavesQuery`
-  **Handler**: `GetLeavesQueryHandler`
-  **Response**: `LeaveResponse`

## Business Rules

-  Results are paginated for performance
-  Leave requests can be filtered by employee, manager, date range, type, and status
-  Includes employee and manager information
-  Results can be sorted by various fields
-  Supports search by employee name or leave type
-  Only shows leaves user has permission to view

## Input Parameters

-  `Page` (int) - Page number for pagination
-  `PageSize` (int) - Number of items per page
-  `EmployeeId` (Guid, optional) - Filter by employee
-  `ManagerId` (Guid, optional) - Filter by manager
-  `StartDate` (DateTime, optional) - Start date for date range filter
-  `EndDate` (DateTime, optional) - End date for date range filter
-  `LeaveType` (LeaveType enum, optional) - Filter by leave type
-  `Status` (LeaveStatus enum, optional) - Filter by leave status
-  `SearchTerm` (string, optional) - Search by employee name
-  `SortBy` (string, optional) - Field to sort by
-  `SortOrder` (SortOrder enum, optional) - Ascending or descending order

## Output

-  `PaginatedResponse<LeaveResponse>` with leave list and pagination metadata
-  Each leave response includes employee and manager information

## Related Entities

-  `Leave` - Main entity being queried
-  `Employee` - Associated employee information
-  `Employee` - Manager information
