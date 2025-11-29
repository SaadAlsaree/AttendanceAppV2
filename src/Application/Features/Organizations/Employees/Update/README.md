# Update Employee

## Description

Updates an existing employee's information including personal details, organizational relationships, and role assignments.

## Operations

-  **Command**: `UpdateEmployeeCommand`
-  **Handler**: `UpdateEmployeeCommandHandler`
-  **Validator**: `UpdateEmployeeCommandValidator`

## Business Rules

-  Employee must exist in the system
-  Employee number cannot be changed to an existing one
-  Email address cannot be changed to an existing one
-  Organizational unit assignment can be updated
-  Manager assignment can be updated
-  Role and permissions can be modified
-  Personal information can be updated
-  Full name is automatically regenerated from name components

## Input Parameters

-  `EmployeeId` (Guid) - Unique identifier of the employee to update
-  `FirstName` (string, optional) - Updated first name
-  `SecondName` (string, optional) - Updated second name
-  `ThirdName` (string, optional) - Updated third name
-  `FourthName` (string, optional) - Updated fourth name
-  `FamilyName` (string, optional) - Updated family name
-  `Email` (string, optional) - Updated email address
-  `PhoneNumber` (string, optional) - Updated phone number
-  `Address` (string, optional) - Updated address
-  `City` (string, optional) - Updated city
-  `Position` (string, optional) - Updated position
-  `JobTitle` (string, optional) - Updated job title
-  `OrganizationalUnitId` (Guid, optional) - Updated organizational unit
-  `ManagerId` (Guid, optional) - Updated manager
-  `IsManager` (bool, optional) - Updated manager status
-  `Role` (Role enum, optional) - Updated role
-  `IsActive` (bool, optional) - Updated active status

## Output

-  `EmployeeResponse` with updated employee details
-  Success/Error result with appropriate messages

## Related Entities

-  `Employee` - Main entity being updated
-  `OrganizationalUnit` - Associated organizational unit
-  `Employee` - Manager relationship
-  `EmployeeUpdatedDomainEvent` - Domain event raised on successful update
