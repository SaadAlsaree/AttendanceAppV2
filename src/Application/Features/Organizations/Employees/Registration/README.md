# Register Employee

## Description

Registers a new employee in the system with personal information, organizational relationships, and creates a corresponding user account.

## Operations

-  **Command**: `RegisterEmployeeCommand`
-  **Handler**: `RegisterEmployeeCommandHandler`
-  **Validator**: `RegisterEmployeeCommandValidator`

## Business Rules

-  Employee number must be unique across the organization
-  Email address must be unique
-  User login must be unique
-  Employee must be assigned to an organizational unit
-  Manager assignment is optional
-  Full name is automatically generated from individual name components
-  Hire date cannot be in the future
-  Birth date must be valid and reasonable (employee must be at least 18 years old)
-  Password must meet security requirements (minimum 8 characters, at least one uppercase letter, one lowercase letter, one number, and one special character)
-  User and employee records are linked and created simultaneously

## Input Parameters

### Employee Information
-  `EmployeeNumber` (string) - Unique employee identifier
-  `FirstName` (string) - Employee's first name
-  `SecondName` (string) - Employee's second name
-  `ThirdName` (string) - Employee's third name (optional)
-  `FourthName` (string) - Employee's fourth name (optional)
-  `FamilyName` (string) - Employee's family name
-  `Email` (string) - Employee's email address
-  `PhoneNumber` (string) - Employee's phone number
-  `Address` (string) - Employee's address
-  `City` (string) - Employee's city
-  `Position` (string) - Employee's position
-  `JobTitle` (string) - Employee's job title
-  `BirthDate` (DateTime) - Employee's birth date
-  `HireDate` (DateTime) - Employee's hire date
-  `OrganizationalUnitId` (Guid) - ID of the organizational unit
-  `ManagerId` (Guid, optional) - ID of the employee's manager
-  `IsManager` (bool) - Whether the employee is a manager
-  `Role` (Role enum) - Employee's role in the system

### User Information
-  `UserLogin` (string) - Unique login identifier for the user
-  `Password` (string) - User's password (will be hashed)
-  `Permissions` (List<Permission>) - List of specific permissions granted to the user
-  `ProfileImageUrl` (string, optional) - URL to user's profile image

## Output

-  `EmployeeResponse` with created employee details
-  Success/Error result with appropriate messages

## Related Entities

-  `Employee` - Main entity being created
-  `User` - User account linked to the employee
-  `OrganizationalUnit` - Associated organizational unit
-  `Employee` - Manager relationship
-  `EmployeeCreatedDomainEvent` - Domain event raised on successful employee creation
-  `UserRegisteredDomainEvent` - Domain event raised on successful user registration
