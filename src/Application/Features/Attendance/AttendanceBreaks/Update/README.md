# Update Attendance Break

## Description

Updates an existing attendance break's information including times, duration, and type.

## Operations

-  **Command**: `UpdateAttendanceBreakCommand`
-  **Handler**: `UpdateAttendanceBreakCommandHandler`
-  **Validator**: `UpdateAttendanceBreakCommandValidator`

## Business Rules

-  Attendance break must exist in the system
-  Cannot update breaks for approved attendance records without proper authorization
-  Break start and end times must be logical (start before end)
-  Break must remain within the attendance period
-  Break type can be updated
-  Duration calculations are updated automatically
-  Cannot create overlapping breaks

## Input Parameters

-  `AttendanceBreakId` (Guid) - Unique identifier of the attendance break to update
-  `BreakType` (BreakType enum, optional) - Updated break type
-  `StartTime` (DateTime, optional) - Updated start time
-  `EndTime` (DateTime, optional) - Updated end time
-  `DurationMinutes` (int, optional) - Updated duration in minutes
-  `Notes` (string, optional) - Updated notes

## Output

-  `AttendanceBreakResponse` with updated break details
-  Success/Error result with appropriate messages

## Related Entities

-  `AttendanceBreak` - Main entity being updated
-  `Attendance` - Associated attendance record
-  `Employee` - Associated employee
