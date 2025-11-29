# Delete Attendance Break

## Description

Deletes an attendance break record from the system. This operation may be soft delete or hard delete based on business requirements.

## Operations

-  **Command**: `DeleteAttendanceBreakCommand`
-  **Handler**: `DeleteAttendanceBreakCommandHandler`
-  **Validator**: `DeleteAttendanceBreakCommandValidator`

## Business Rules

-  Attendance break must exist in the system
-  Cannot delete breaks for approved attendance records without proper authorization
-  System may perform soft delete instead of hard delete
-  Associated attendance record's break minutes are recalculated
-  Deletion is logged for audit purposes

## Input Parameters

-  `AttendanceBreakId` (Guid) - Unique identifier of the attendance break to delete

## Output

-  Success/Error result with appropriate messages
-  Confirmation of deletion operation

## Related Entities

-  `AttendanceBreak` - Main entity being deleted
-  `Attendance` - Associated attendance record (break minutes recalculated)
