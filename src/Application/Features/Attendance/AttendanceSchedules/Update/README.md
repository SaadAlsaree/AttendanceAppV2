# Update Attendance Schedule

## Description

Updates an existing attendance schedule's information including working hours, break times, and rules.

## Operations

-  **Command**: `UpdateAttendanceScheduleCommand`
-  **Handler**: `UpdateAttendanceScheduleCommandHandler`
-  **Validator**: `UpdateAttendanceScheduleCommandValidator`

## Business Rules

-  Attendance schedule must exist in the system
-  Name must remain unique within the organization
-  Working hours must be logical (start before end)
-  Break times must be within working hours
-  Cannot update if schedule is actively used by employees
-  Schedule type can be updated
-  Working days and times can be modified
-  Grace period and overtime rules can be updated

## Input Parameters

-  `AttendanceScheduleId` (Guid) - Unique identifier of the attendance schedule to update
-  `Name` (string, optional) - Updated schedule name
-  `Description` (string, optional) - Updated schedule description
-  `ScheduleType` (ScheduleType enum, optional) - Updated schedule type
-  `WorkingDays` (List<DayOfWeek>, optional) - Updated working days
-  `StartTime` (TimeSpan, optional) - Updated daily start time
-  `EndTime` (TimeSpan, optional) - Updated daily end time
-  `BreakStartTime` (TimeSpan, optional) - Updated break start time
-  `BreakEndTime` (TimeSpan, optional) - Updated break end time
-  `GracePeriodMinutes` (int, optional) - Updated grace period
-  `OvertimeStartMinutes` (int, optional) - Updated overtime start minutes
-  `IsActive` (bool, optional) - Updated active status

## Output

-  `AttendanceScheduleResponse` with updated schedule details
-  Success/Error result with appropriate messages

## Related Entities

-  `AttendanceSchedule` - Main entity being updated
-  `OrganizationalUnit` - Associated organization
