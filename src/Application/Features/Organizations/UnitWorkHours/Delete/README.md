# Delete Unit Work Hours

## Description

Deletes a unit work hours configuration from the system. This operation may be soft delete or hard delete based on business requirements.

## Operations

-  **Command**: `DeleteUnitWorkHoursCommand`
-  **Handler**: `DeleteUnitWorkHoursCommandHandler`
-  **Validator**: `DeleteUnitWorkHoursCommandValidator`

## Business Rules

-  Unit work hours must exist in the system
-  Cannot delete configurations that are actively used by employees
-  Cannot delete configurations with associated attendance records
-  System may perform soft delete instead of hard delete
-  Associated employee schedules may need to be updated
-  Deletion is logged for audit purposes

## Input Parameters

-  `UnitWorkHoursId` (Guid) - Unique identifier of the unit work hours to delete

## Output

-  Success/Error result with appropriate messages
-  Confirmation of deletion operation

## Related Entities

-  `UnitWorkHours` - Main entity being deleted
-  `Employee` - Associated employees that may need schedule updates
-  `Attendance` - Associated attendance records that may prevent deletion
