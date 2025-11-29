# User SignUp

## Description

Registers a new user in the system with proper validation and password hashing.

## Operations

-  **Command**: `SignUpCommand`
-  **Handler**: `SignUpCommandHandler`
-  **Validator**: `SignUpCommandValidator`

## Business Rules

-  User login must be unique in the system
-  Password must meet security requirements (length, complexity)
-  Password confirmation must match the password
-  User is created with default active status
-  Password is properly hashed before storage
-  User role defaults to User unless specified
-  Created date and last login date are set to current time

## Input Parameters

-  `UserLogin` (string) - Unique user login identifier
-  `Password` (string) - User password (must meet complexity requirements)
-  `ConfirmPassword` (string) - Password confirmation (must match password)
-  `Role` (Role, optional) - User role (defaults to User)

## Output

-  `SignUpResponse` with user information and creation details
-  Success/Error result with appropriate messages

## Validation Rules

-  User login: Required, max 100 characters, alphanumeric + underscore only
-  Password: Required, min 8 characters, must contain uppercase, lowercase, number, and special character
-  Confirm password: Required, must match password exactly
-  Role: Must be a valid enum value

## Related Entities

-  `User` - Main entity being created
-  `IPasswordHasher` - Service for password hashing
-  `IDateTimeProvider` - Service for current date/time
-  `UserErrors` - Error definitions for user operations

