# Change User Password

## Description

Allows users to change their password with proper validation and security measures.

## Operations

-  **Command**: `ChangePasswordCommand`
-  **Handler**: `ChangePasswordCommandHandler`
-  **Validator**: `ChangePasswordCommandValidator`

## Business Rules

-  User must exist and be active
-  Current password must be verified
-  New password must meet security requirements
-  New password cannot be the same as current password
-  Password change is logged for security audit
-  Password hash is updated in the database

## Input Parameters

-  `UserId` (Guid) - Unique identifier of the user
-  `CurrentPassword` (string) - Current password for verification
-  `NewPassword` (string) - New password to set
-  `ConfirmPassword` (string) - Confirmation of new password

## Output

-  Success/Error result with appropriate messages
-  Confirmation of password change

## Related Entities

-  `User` - Main entity being updated
-  `IPasswordHasher` - Service for password hashing
-  `SecurityAuditLog` - Audit trail for security events
