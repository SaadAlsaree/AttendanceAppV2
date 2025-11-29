# Get Employee By ID

## Description

Retrieves a specific employee by their unique identifier with complete details and relationships.

## Operations

-  **Query**: `GetEmployeeByIdQuery`
-  **Handler**: `GetEmployeeByIdQueryHandler`
-  **Response**: `EmployeeResponse`

## Business Rules

-  Employee must exist in the system
-  Returns complete employee information including relationships
-  Includes organizational unit details
-  Includes manager information
-  Includes subordinate employees (if manager)
-  Includes managed organizational units (if manager)

## Input Parameters

-  `EmployeeId` (Guid) - Unique identifier of the employee to retrieve

## Output

-  `EmployeeResponse` with complete employee details and relationships
-  Error result if employee is not found

## Related Entities

-  `Employee` - Main entity being queried
-  `OrganizationalUnit` - Associated organizational unit
-  `Employee` - Manager and subordinate relationships
-  `OrganizationalUnit` - Managed units (if manager)
