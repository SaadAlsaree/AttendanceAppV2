# Get Employees

## Description

Retrieves a paginated list of employees with optional filtering, sorting, and relationship data.

## Operations

-  **Query**: `GetEmployeesQuery`
-  **Handler**: `GetEmployeesQueryHandler`
-  **Response**: `EmployeeResponse`

## Business Rules

-  Only active employees are returned by default
-  Results are paginated for performance
-  Employees can be filtered by organizational unit, manager, role, and status
-  Includes organizational unit and manager information
-  Results can be sorted by various fields
-  Supports search by name, employee number, or email

## Input Parameters

-  `Page` (int) - Page number for pagination
-  `PageSize` (int) - Number of items per page
-  `SearchTerm` (string, optional) - Search by name, employee number, or email
-  `OrganizationalUnitId` (Guid, optional) - Filter by organizational unit
-  `ManagerId` (Guid, optional) - Filter by manager
-  `Status` (UserStatus enum, optional) - Filter by status
-  `IsManager` (bool, optional) - Filter by manager status
-  `SortBy` (string, optional) - Field to sort by
-  `SortOrder` (SortOrder enum, optional) - Ascending or descending order

## Output

-  `PaginatedResponse<EmployeeResponse>` with employee list and pagination metadata
-  Each employee response includes organizational unit and manager information

## Related Entities

-  `Employee` - Main entity being queried
-  `OrganizationalUnit` - Associated organizational unit information
-  `Employee` - Manager relationship information
