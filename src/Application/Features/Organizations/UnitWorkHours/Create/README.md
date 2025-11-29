# Create Unit Work Hours

## Description

Creates working hours configuration for an organizational unit with specific schedules and rules.

## Operations

-  **Command**: `CreateUnitWorkHoursCommand`
-  **Handler**: `CreateUnitWorkHoursCommandHandler`
-  **Validator**: `CreateUnitWorkHoursCommandValidator`

## Business Rules

-  Organizational unit must exist
-  Working hours must be logical (start before end)
-  Days of the week must be specified
-  Configuration must be unique for the unit
-  Break times must be within working hours
-  Overtime rules can be configured

## Input Parameters

-  `OrganizationalUnitId` (Guid) - ID of the organizational unit
-  `DayOfWeek` (DayOfWeek enum) - Day of the week
-  `StartTime` (TimeSpan) - Working start time
-  `EndTime` (TimeSpan) - Working end time
-  `BreakStartTime` (TimeSpan, optional) - Break start time
-  `BreakEndTime` (TimeSpan, optional) - Break end time
-  `IsWorkingDay` (bool) - Whether it's a working day
-  `GracePeriodMinutes` (int, optional) - Grace period for late arrivals
-  `OvertimeStartMinutes` (int, optional) - Minutes after end time for overtime

## Output

-  `UnitWorkHoursResponse` with created work hours details
-  Success/Error result with appropriate messages

## Related Entities

-  `UnitWorkHours` - Main entity being created
-  `OrganizationalUnit` - Associated organizational unit
