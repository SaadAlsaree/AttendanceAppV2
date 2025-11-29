# Update Shift

## Description

Updates an existing shift's information including working hours, break times, and rules.

## Operations

-  **Command**: `UpdateShiftCommand`
-  **Handler**: `UpdateShiftCommandHandler`
-  **Validator**: `UpdateShiftCommandValidator`

## Business Rules

-  Shift must exist in the system
-  Name must remain unique within the organization
-  Working hours must be logical (start before end)
-  Break times must be within working hours
-  Cannot update if shift is actively used by employees
-  Shift type can be updated
-  Grace period and overtime rules can be modified

## Input Parameters

-  `ShiftId` (Guid) - Unique identifier of the shift to update
-  `Name` (string, optional) - Updated shift name
-  `Description` (string, optional) - Updated shift description
-  `ShiftType` (ShiftType enum, optional) - Updated shift type
-  `StartTime` (TimeSpan, optional) - Updated start time
-  `EndTime` (TimeSpan, optional) - Updated end time
-  `BreakStartTime` (TimeSpan, optional) - Updated break start time
-  `BreakEndTime` (TimeSpan, optional) - Updated break end time
-  `GracePeriodMinutes` (int, optional) - Updated grace period
-  `OvertimeStartMinutes` (int, optional) - Updated overtime start minutes
-  `IsActive` (bool, optional) - Updated active status

## Output

-  `ShiftResponse` with updated shift details
-  Success/Error result with appropriate messages

## Related Entities

-  `Shift` - Main entity being updated
-  `OrganizationalUnit` - Associated organization
