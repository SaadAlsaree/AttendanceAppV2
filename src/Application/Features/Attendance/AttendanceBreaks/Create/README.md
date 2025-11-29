# Create Attendance Break

## Description

Creates a new break record for an employee during their attendance period, tracking break start and end times.

## Operations

-  **Command**: `CreateAttendanceBreakCommand`
-  **Handler**: `CreateAttendanceBreakCommandHandler`
-  **Validator**: `CreateAttendanceBreakCommandValidator`

## Business Rules

-  Employee must have an active attendance record for the day
-  Break start time must be within the attendance period
-  Break start time cannot be in the future
-  Break type must be valid (Lunch, Coffee, Rest, etc.)
-  Break duration must be reasonable and within policy limits
-  Cannot create overlapping breaks for the same employee
-  Break end time is calculated based on duration or manually set

## Input Parameters

-  `AttendanceId` (Guid) - ID of the attendance record
-  `EmployeeId` (Guid) - ID of the employee
-  `BreakType` (BreakType enum) - Type of break (Lunch, Coffee, Rest, etc.)
-  `StartTime` (DateTime) - Break start time
-  `EndTime` (DateTime, optional) - Break end time (if not using duration)
-  `DurationMinutes` (int, optional) - Break duration in minutes
-  `Notes` (string, optional) - Additional notes about the break

## Output

-  `AttendanceBreakResponse` with created break details
-  Success/Error result with appropriate messages

## Related Entities

-  `AttendanceBreak` - Main entity being created
-  `Attendance` - Associated attendance record
-  `Employee` - Associated employee
