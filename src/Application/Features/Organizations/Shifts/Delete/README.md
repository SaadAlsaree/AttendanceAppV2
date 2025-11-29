# Delete Shift

## Description

Deletes a shift from the system. This operation may be soft delete or hard delete based on business requirements.

## Operations

-  **Command**: `DeleteShiftCommand`
-  **Handler**: `DeleteShiftCommandHandler`
-  **Validator**: `DeleteShiftCommandValidator`

## Business Rules

-  Shift must exist in the system
-  Cannot delete shifts that are actively used by employees
-  Cannot delete shifts with associated attendance records
-  System may perform soft delete instead of hard delete
-  Associated employee assignments may need to be updated
-  Deletion is logged for audit purposes

## Input Parameters

-  `ShiftId` (Guid) - Unique identifier of the shift to delete

## Output

-  Success/Error result with appropriate messages
-  Confirmation of deletion operation

## Related Entities

-  `Shift` - Main entity being deleted
-  `Employee` - Associated employees that may need reassignment
-  `Attendance` - Associated attendance records that may prevent deletion
