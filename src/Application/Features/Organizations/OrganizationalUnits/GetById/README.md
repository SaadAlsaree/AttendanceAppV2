# Get Organizational Unit By ID

## Description

Retrieves a specific organizational unit by its unique identifier with complete hierarchical details.

## Operations

-  **Query**: `GetOrganizationalUnitByIdQuery`
-  **Handler**: `GetOrganizationalUnitByIdQueryHandler`
-  **Response**: `OrganizationalUnitResponse`

## Business Rules

-  Organizational unit must exist in the system
-  Returns complete unit information including hierarchy
-  Includes parent and child unit information
-  Includes manager and employee details
-  Includes employee count and structure information

## Input Parameters

-  `OrganizationalUnitId` (Guid) - Unique identifier of the organizational unit to retrieve

## Output

-  `OrganizationalUnitResponse` with complete unit details and hierarchy
-  Error result if organizational unit is not found

## Related Entities

-  `OrganizationalUnit` - Main entity being queried
-  `OrganizationalUnit` - Parent and child units
-  `Employee` - Manager and employee information
