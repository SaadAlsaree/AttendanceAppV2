# Update Schedule Days

## Description

Updates the schedule days for an existing attendance schedule, allowing modification of shift assignments, active status, and notes for specific days of the week.

## Operations

-  **Command**: `UpdateScheduleDaysCommand`
-  **Handler**: `UpdateScheduleDaysCommandHandler`
-  **Validator**: `UpdateScheduleDaysCommandValidator`

## Business Rules

-  Attendance schedule must exist in the system
-  All schedule day IDs must belong to the specified attendance schedule
-  All referenced shifts must exist in the system
-  Schedule days can be updated with new shift assignments
-  Active status can be modified for each schedule day
-  Notes can be updated for each schedule day
-  Day of week cannot be modified (it's part of the entity identity)

## Input Parameters

-  `AttendanceScheduleId` (Guid) - Unique identifier of the attendance schedule
-  `ScheduleDays` (List<UpdateScheduleDayCommand>) - List of schedule days to update
-  `Id` (Guid) - Unique identifier of the schedule day
-  `ShiftId` (Guid) - New shift ID to assign to this schedule day
-  `IsActive` (bool) - Whether this schedule day is active
-  `Notes` (string, optional) - Notes for this schedule day

## Output

-  `bool` - Success indicator (true if update was successful)
-  Success/Error result with appropriate messages

## Related Entities

-  `AttendanceSchedule` - Parent entity being referenced
-  `ScheduleDay` - Entity being updated
-  `Shift` - Referenced entity for shift assignments

## Error Scenarios

-  Attendance schedule not found
-  Schedule day not found or doesn't belong to the specified schedule
-  Shift not found
-  Validation errors (empty IDs, invalid notes length, etc.)
