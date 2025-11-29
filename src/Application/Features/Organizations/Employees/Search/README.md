# Search Employees

## Description

Searches for employees based on search terms with optional filtering, sorting, and relationship data. This feature is optimized for search functionality with enhanced search capabilities across multiple fields.

## Operations

-  **Query**: `SearchEmployeeQuery`
-  **Handler**: `SearchEmployeeHandler`
-  **Response**: `EmployeeResponse`

## Business Rules

-  Search term is required for meaningful results
-  Results are paginated for performance
-  Employees can be filtered by organizational unit and manager status
-  Includes organizational unit and manager information
-  Supports search across name, employee code, and email fields
-  Search is case-insensitive and supports partial matches

## Input Parameters

-  `Page` (int) - Page number for pagination
-  `PageSize` (int) - Number of items per page
-  `SearchTerm` (string, required) - Search by name, employee code, or email
-  `OrganizationalUnitId` (Guid, optional) - Filter by organizational unit
-  `IsManager` (bool, optional) - Filter by manager status

## Output

-  `PaginatedResponse<EmployeeResponse>` with employee list and pagination metadata
-  Each employee response includes organizational unit and manager information

## Search Capabilities

-  **Full Name**: Searches in employee's full name
-  **Employee Code**: Searches in employee's code field
-  **Email**: Searches in employee's email address
-  **Partial Matching**: Supports partial text matching with wildcards
-  **Case Insensitive**: Search is not case-sensitive

## Related Entities

-  `Employee` - Main entity being searched
-  `OrganizationalUnit` - Associated organizational unit information
-  `Employee` - Manager relationship information
-  `User` - User status and authentication information

## Usage Example

```csharp
var searchQuery = new SearchEmployeeQuery
{
    SearchTerm = "John",
    Page = 1,
    PageSize = 10,
    OrganizationalUnitId = Guid.Parse("..."),
    IsManager = false
};

var result = await mediator.Send(searchQuery);
```

