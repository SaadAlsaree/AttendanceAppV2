# Delete Attendance Schedule

## Description

Deletes an attendance schedule from the system. This operation may be soft delete or hard delete based on business requirements.

## Operations

-  **Command**: `DeleteAttendanceScheduleCommand`
-  **Handler**: `DeleteAttendanceScheduleCommandHandler`
-  **Validator**: `DeleteAttendanceScheduleCommandValidator`

## Business Rules

-  Attendance schedule must exist in the system
-  Cannot delete schedules that are actively used by employees
-  Cannot delete schedules with associated attendance records
-  System may perform soft delete instead of hard delete
-  Associated employee assignments may need to be updated
-  Deletion is logged for audit purposes

## Input Parameters

-  `AttendanceScheduleId` (Guid) - Unique identifier of the attendance schedule to delete

## Output

-  Success/Error result with appropriate messages
-  Confirmation of deletion operation

## Related Entities

-  `AttendanceSchedule` - Main entity being deleted
-  `Employee` - Associated employees that may need reassignment
-  `Attendance` - Associated attendance records that may prevent deletion
