# Update Unit Work Hours

## Description

Updates an existing unit work hours configuration including working hours, break times, and rules.

## Operations

-  **Command**: `UpdateUnitWorkHoursCommand`
-  **Handler**: `UpdateUnitWorkHoursCommandHandler`
-  **Validator**: `UpdateUnitWorkHoursCommandValidator`

## Business Rules

-  Unit work hours must exist in the system
-  Working hours must be logical (start before end)
-  Break times must be within working hours
-  Cannot update if configuration is actively used by employees
-  Day of week cannot be changed
-  Grace period and overtime rules can be modified

## Input Parameters

-  `UnitWorkHoursId` (Guid) - Unique identifier of the unit work hours to update
-  `StartTime` (TimeSpan, optional) - Updated start time
-  `EndTime` (TimeSpan, optional) - Updated end time
-  `BreakStartTime` (TimeSpan, optional) - Updated break start time
-  `BreakEndTime` (TimeSpan, optional) - Updated break end time
-  `IsWorkingDay` (bool, optional) - Updated working day status
-  `GracePeriodMinutes` (int, optional) - Updated grace period
-  `OvertimeStartMinutes` (int, optional) - Updated overtime start minutes

## Output

-  `UnitWorkHoursResponse` with updated work hours details
-  Success/Error result with appropriate messages

## Related Entities

-  `UnitWorkHours` - Main entity being updated
-  `OrganizationalUnit` - Associated organizational unit
