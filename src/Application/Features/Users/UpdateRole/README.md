# Update User Role

## Description

Allows administrators to update a user's role within the system, changing their permissions and access levels.

## Operations

-  **Command**: `UpdateUserRoleCommand`
-  **Handler**: `UpdateUserRoleCommandHandler`
-  **Validator**: `UpdateUserRoleCommandValidator`

## Business Rules

-  User must exist and be active
-  Only administrators can update roles
-  Role must be a valid system role
-  Role change is logged for security audit
-  User's role is updated in the database

## Input Parameters

-  `UserId` (Guid) - Unique identifier of the user
-  `NewRole` (string) - New role to assign to the user
-  `UpdatedBy` (Guid) - ID of the administrator making the change

## Output

-  Success/Error result with appropriate messages
-  Confirmation of role change

## Related Entities

-  `User` - Main entity being updated
-  `SecurityAuditLog` - Audit trail for security events
