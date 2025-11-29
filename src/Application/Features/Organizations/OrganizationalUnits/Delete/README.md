# Delete Organizational Unit

## Description

Deletes an organizational unit from the system. This operation may be soft delete or hard delete based on business requirements.

## Operations

-  **Command**: `DeleteOrganizationalUnitCommand`
-  **Handler**: `DeleteOrganizationalUnitCommandHandler`
-  **Validator**: `DeleteOrganizationalUnitCommandValidator`

## Business Rules

-  Organizational unit must exist in the system
-  Cannot delete units with active employees
-  Cannot delete units with child units
-  System may perform soft delete instead of hard delete
-  Associated employees may need to be reassigned
-  Deletion is logged for audit purposes

## Input Parameters

-  `OrganizationalUnitId` (Guid) - Unique identifier of the organizational unit to delete

## Output

-  Success/Error result with appropriate messages
-  Confirmation of deletion operation

## Related Entities

-  `OrganizationalUnit` - Main entity being deleted
-  `Employee` - Associated employees that may need reassignment
-  `OrganizationalUnit` - Child units that may prevent deletion
