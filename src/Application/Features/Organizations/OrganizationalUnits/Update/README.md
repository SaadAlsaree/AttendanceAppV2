# Update Organizational Unit

## Description

Updates an existing organizational unit's information including structure and management.

## Operations

-  **Command**: `UpdateOrganizationalUnitCommand`
-  **Handler**: `UpdateOrganizationalUnitCommandHandler`
-  **Validator**: `UpdateOrganizationalUnitCommandValidator`

## Business Rules

-  Organizational unit must exist in the system
-  Name must remain unique within the organization
-  Code must remain unique within the organization
-  Cannot create circular parent-child relationships
-  Manager assignment can be updated
-  Parent unit can be changed with proper validation

## Input Parameters

-  `OrganizationalUnitId` (Guid) - Unique identifier of the organizational unit to update
-  `Name` (string, optional) - Updated unit name
-  `Code` (string, optional) - Updated unit code
-  `Description` (string, optional) - Updated unit description
-  `ParentUnitId` (Guid, optional) - Updated parent unit
-  `ManagerId` (Guid, optional) - Updated unit manager
-  `IsActive` (bool, optional) - Updated active status

## Output

-  `OrganizationalUnitResponse` with updated unit details
-  Success/Error result with appropriate messages

## Related Entities

-  `OrganizationalUnit` - Main entity being updated
-  `OrganizationalUnit` - Parent unit
-  `Employee` - Manager
