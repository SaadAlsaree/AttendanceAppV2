# Create Shift

## Description

Creates a new work shift with defined working hours, break times, and scheduling rules.

## Operations

-  **Command**: `CreateShiftCommand`
-  **Handler**: `CreateShiftCommandHandler`
-  **Validator**: `CreateShiftCommandValidator`

## Business Rules

-  Shift name must be unique within the organization
-  Organization must exist
-  Working hours must be logical (start before end)
-  Break times must be within working hours
-  Shift type must be valid (Regular, Night, Split, etc.)
-  Grace period and overtime rules can be configured

## Input Parameters

-  `OrganizationId` (Guid) - ID of the organization
-  `Name` (string) - Shift name
-  `Description` (string, optional) - Shift description
-  `ShiftType` (ShiftType enum) - Type of shift
-  `StartTime` (TimeSpan) - Shift start time
-  `EndTime` (TimeSpan) - Shift end time
-  `BreakStartTime` (TimeSpan, optional) - Break start time
-  `BreakEndTime` (TimeSpan, optional) - Break end time
-  `GracePeriodMinutes` (int, optional) - Grace period for late arrivals
-  `OvertimeStartMinutes` (int, optional) - Minutes after end time for overtime
-  `IsActive` (bool) - Whether shift is active

## Output

-  `ShiftResponse` with created shift details
-  Success/Error result with appropriate messages

## Related Entities

-  `Shift` - Main entity being created
-  `OrganizationalUnit` - Associated organization
