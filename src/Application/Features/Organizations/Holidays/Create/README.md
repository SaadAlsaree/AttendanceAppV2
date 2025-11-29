# Create Holiday

## Description

Creates a new holiday record for an organization, defining non-working days and special occasions.

## Operations

-  **Command**: `CreateHolidayCommand`
-  **Handler**: `CreateHolidayCommandHandler`
-  **Validator**: `CreateHolidayCommandValidator`

## Business Rules

-  Holiday name must be provided
-  Date must be valid and not in the past
-  Organization must exist
-  Holiday type must be valid (Public, Company, Religious, etc.)
-  Date must be unique within the organization
-  Description is optional but recommended

## Input Parameters

-  `OrganizationId` (Guid) - ID of the organization
-  `Name` (string) - Holiday name
-  `Date` (DateTime) - Holiday date
-  `Description` (string, optional) - Holiday description
-  `IsRecurring` (bool, optional) - Whether holiday recurs annually
-  `IsPaid` (bool, optional) - Whether it's a paid holiday

## Output

-  `HolidayResponse` with created holiday details
-  Success/Error result with appropriate messages

## Related Entities

-  `Holiday` - Main entity being created
-  `OrganizationalUnit` - Associated organization
