# Create Organizational Unit

## Description

Creates a new organizational unit (department, division, etc.) within the organization structure.

## Operations

-  **Command**: `CreateOrganizationalUnitCommand`
-  **Handler**: `CreateOrganizationalUnitCommandHandler`
-  **Validator**: `CreateOrganizationalUnitCommandValidator`

## Business Rules

-  Unit name must be unique within the organization
-  Organization must exist
-  Parent unit must exist (if specified)
-  Unit type must be valid (Department, Division, Team, etc.)
-  Unit code must be unique within the organization
-  Manager assignment is optional

## Input Parameters

-  `OrganizationId` (Guid) - ID of the organization
-  `Name` (string) - Unit name
-  `Code` (string) - Unit code/identifier
-  `Description` (string, optional) - Unit description
-  `ParentUnitId` (Guid, optional) - ID of parent unit
-  `ManagerId` (Guid, optional) - ID of unit manager
-  `IsActive` (bool) - Whether unit is active

## Output

-  `OrganizationalUnitResponse` with created unit details
-  Success/Error result with appropriate messages

## Related Entities

-  `OrganizationalUnit` - Main entity being created
-  `OrganizationalUnit` - Parent unit (if applicable)
-  `Employee` - Manager (if assigned)
