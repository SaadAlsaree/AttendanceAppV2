# Delete Work Location

## Description

Deletes a work location from the system with proper validation to ensure data integrity.

## Operations

-  **Command**: `DeleteWorkLocationCommand`
-  **Handler**: `DeleteWorkLocationCommandHandler`
-  **Validator**: `DeleteWorkLocationCommandValidator`

## Business Rules

-  Work location must exist in the system
-  Cannot delete work location if it has associated devices
-  Cannot delete work location if it has associated attendance records
-  Performs soft delete by removing the entity from the database
-  Maintains referential integrity

## Input Parameters

-  `Id` (Guid) - ID of the work location to delete

## Output

-  Success/Error result with appropriate messages

## Validation Checks

-  Work location existence validation
-  Device association check - prevents deletion if devices are linked
-  Attendance record association check - prevents deletion if attendance records reference the location

## Related Entities

-  `WorkLocation` - Main entity being deleted
-  `Device` - Associated devices that prevent deletion
-  `Attendance` - Associated attendance records that prevent deletion

## Error Scenarios

-  Work location not found
-  Work location has associated devices
-  Work location has associated attendance records
