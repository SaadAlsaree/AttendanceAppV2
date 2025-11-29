# Create Attendance Schedule

## Description

Creates a new attendance schedule defining working hours, break times, and attendance rules for employees.

## Operations

-  **Command**: `CreateAttendanceScheduleCommand`
-  **Handler**: `CreateAttendanceScheduleCommandHandler`
-  **Validator**: `CreateAttendanceScheduleCommandValidator`

## Business Rules

-  Schedule name must be unique within the organization
-  Organization must exist
-  Working hours must be logical (start before end)
-  Break times must be within working hours
-  Schedule type must be valid (Regular, Flexible, Shift-based, etc.)
-  Days of the week must be specified
-  Grace period for late arrivals can be set
-  Overtime rules can be configured

## Input Parameters

-  `OrganizationId` (Guid) - ID of the organization
-  `Name` (string) - Schedule name
-  `Description` (string, optional) - Schedule description
-  `ScheduleType` (ScheduleType enum) - Type of schedule
-  `WorkingDays` (List<DayOfWeek>) - Days when schedule applies
-  `StartTime` (TimeSpan) - Daily start time
-  `EndTime` (TimeSpan) - Daily end time
-  `BreakStartTime` (TimeSpan, optional) - Break start time
-  `BreakEndTime` (TimeSpan, optional) - Break end time
-  `GracePeriodMinutes` (int, optional) - Grace period for late arrivals
-  `OvertimeStartMinutes` (int, optional) - Minutes after end time for overtime
-  `IsActive` (bool) - Whether schedule is active

## Output

-  `AttendanceScheduleResponse` with created schedule details
-  Success/Error result with appropriate messages

## Related Entities

-  `AttendanceSchedule` - Main entity being created
-  `OrganizationalUnit` - Associated organization
