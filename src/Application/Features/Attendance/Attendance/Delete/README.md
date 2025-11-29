# Delete Attendance Record

## Description

Deletes an attendance record from the system. This operation may be soft delete or hard delete based on business requirements.

## Operations

-  **Command**: `DeleteAttendanceCommand`
-  **Handler**: `DeleteAttendanceCommandHandler`
-  **Validator**: `DeleteAttendanceCommandValidator`

## Business Rules

-  Attendance record must exist in the system
-  Cannot delete approved attendance records without proper authorization
-  Cannot delete attendance records with associated break records
-  Cannot delete attendance records with associated attendance logs
-  System may perform soft delete instead of hard delete
-  Associated records may need to be cleaned up

## Input Parameters

-  `AttendanceId` (Guid) - Unique identifier of the attendance record to delete

## Output

-  Success/Error result with appropriate messages
-  Confirmation of deletion operation

## Related Entities

-  `Attendance` - Main entity being deleted
-  `AttendanceBreak` - Associated break records that may prevent deletion
-  `AttendanceLog` - Associated log records that may prevent deletion
