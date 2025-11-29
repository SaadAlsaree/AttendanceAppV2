# Delete Employee

## Description

Deletes an employee record from the system. This operation may be soft delete (deactivation) or hard delete based on business requirements.

## Operations

-  **Command**: `DeleteEmployeeCommand`
-  **Handler**: `DeleteEmployeeCommandHandler`
-  **Validator**: `DeleteEmployeeCommandValidator`

## Business Rules

-  Employee must exist in the system
-  Cannot delete employees with active attendance records
-  Cannot delete employees who are managers of other employees
-  Cannot delete employees with pending leave requests
-  System may perform soft delete (deactivation) instead of hard delete
-  Associated user account may need to be handled

## Input Parameters

-  `EmployeeId` (Guid) - Unique identifier of the employee to delete

## Output

-  Success/Error result with appropriate messages
-  Confirmation of deletion operation

## Related Entities

-  `Employee` - Main entity being deleted
-  `User` - Associated user account
-  `Attendance` - Attendance records that may prevent deletion
-  `Leave` - Leave requests that may prevent deletion
-  `Employee` - Subordinate employees that may prevent deletion
