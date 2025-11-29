# Assign Manager to Employee

## Description

Assigns or changes the manager for a specific employee, updating the organizational hierarchy.

## Operations

-  **Command**: `AssignManagerCommand`
-  **Handler**: `AssignManagerCommandHandler`
-  **Validator**: `AssignManagerCommandValidator`

## Business Rules

-  Employee must exist in the system
-  Manager must exist and be active
-  Manager cannot be the same as the employee
-  Manager must have appropriate permissions
-  Cannot create circular management relationships
-  Employee's IsManager status may be updated automatically
-  Previous manager relationship is properly updated

## Input Parameters

-  `EmployeeId` (Guid) - Unique identifier of the employee
-  `ManagerId` (Guid) - Unique identifier of the new manager
-  `EffectiveDate` (DateTime, optional) - When the assignment becomes effective

## Output

-  Success/Error result with appropriate messages
-  Confirmation of manager assignment

## Related Entities

-  `Employee` - Employee being assigned a manager
-  `Employee` - New manager
-  `EmployeeManagerAssignedDomainEvent` - Domain event raised on successful assignment
