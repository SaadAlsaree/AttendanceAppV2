# Get Holiday By ID

## Description

Retrieves a specific holiday by its unique identifier with complete details and relationships.

## Operations

-  **Query**: `GetHolidayByIdQuery`
-  **Handler**: `GetHolidayByIdQueryHandler`
-  **Response**: `HolidayResponse`

## Business Rules

-  Holiday must exist in the system
-  Returns complete holiday information including relationships
-  Includes organization details
-  Holiday date and type information is included

## Input Parameters

-  `HolidayId` (Guid) - Unique identifier of the holiday to retrieve

## Output

-  `HolidayResponse` with complete holiday details and relationships
-  Error result if holiday is not found

## Related Entities

-  `Holiday` - Main entity being queried
-  `OrganizationalUnit` - Associated organization information
