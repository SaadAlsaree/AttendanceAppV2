# Update Holiday

## Description

Updates an existing holiday's information including name, date, and properties.

## Operations

-  **Command**: `UpdateHolidayCommand`
-  **Handler**: `UpdateHolidayCommandHandler`
-  **Validator**: `UpdateHolidayCommandValidator`

## Business Rules

-  Holiday must exist in the system
-  Date must remain unique within the organization
-  Date cannot be changed to a past date
-  Name and description can be updated
-  Recurring and paid status can be modified
-  Cannot update if holiday has already passed

## Input Parameters

-  `HolidayId` (Guid) - Unique identifier of the holiday to update
-  `Name` (string, optional) - Updated holiday name
-  `Date` (DateTime, optional) - Updated holiday date
-  `Description` (string, optional) - Updated holiday description
-  `IsRecurring` (bool, optional) - Updated recurring status
-  `IsPaid` (bool, optional) - Updated paid status

## Output

-  `HolidayResponse` with updated holiday details
-  Success/Error result with appropriate messages

## Related Entities

-  `Holiday` - Main entity being updated
-  `OrganizationalUnit` - Associated organization
