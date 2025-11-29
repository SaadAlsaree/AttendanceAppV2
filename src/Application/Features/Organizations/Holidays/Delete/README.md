# Delete Holiday

## Description

Deletes a holiday from the system. This operation may be soft delete or hard delete based on business requirements.

## Operations

-  **Command**: `DeleteHolidayCommand`
-  **Handler**: `DeleteHolidayCommandHandler`
-  **Validator**: `DeleteHolidayCommandValidator`

## Business Rules

-  Holiday must exist in the system
-  Cannot delete holidays that have already passed
-  System may perform soft delete instead of hard delete
-  Deletion is logged for audit purposes
-  Associated attendance records may need to be updated

## Input Parameters

-  `HolidayId` (Guid) - Unique identifier of the holiday to delete

## Output

-  Success/Error result with appropriate messages
-  Confirmation of deletion operation

## Related Entities

-  `Holiday` - Main entity being deleted
-  `Attendance` - Associated attendance records that may be affected
