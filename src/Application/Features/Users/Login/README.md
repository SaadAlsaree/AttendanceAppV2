# User Login

## Description

Authenticates a user and provides access tokens for system access.

## Operations

-  **Command**: `LoginCommand`
-  **Handler**: `LoginCommandHandler`
-  **Validator**: `LoginCommandValidator`

## Business Rules

-  User must exist and be active
-  Password must match the stored hash
-  Failed login attempts may be tracked
-  JWT token is generated upon successful authentication
-  Last login date is updated
-  User status is verified

## Input Parameters

-  `UserLogin` (string) - User login identifier
-  `Password` (string) - User password
-  `RememberMe` (bool, optional) - Whether to extend token lifetime

## Output

-  `LoginResponse` with authentication token and user information
-  Success/Error result with appropriate messages

## Related Entities

-  `User` - Main entity being authenticated
-  `ITokenProvider` - Service for token generation
-  `IPasswordHasher` - Service for password verification
