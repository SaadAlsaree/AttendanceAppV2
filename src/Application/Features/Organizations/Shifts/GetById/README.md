# Get Shift By ID

## Description

Retrieves a specific shift by its unique identifier with complete details and relationships.

## Operations

-  **Query**: `GetShiftByIdQuery`
-  **Handler**: `GetShiftByIdQueryHandler`
-  **Response**: `ShiftResponse`

## Business Rules

-  Shift must exist in the system
-  Returns complete shift information including relationships
-  Includes organization details
-  Includes working hours and break configuration
-  Includes overtime and grace period settings

## Input Parameters

-  `ShiftId` (Guid) - Unique identifier of the shift to retrieve

## Output

-  `ShiftResponse` with complete shift details and relationships
-  Error result if shift is not found

## Related Entities

-  `Shift` - Main entity being queried
-  `OrganizationalUnit` - Associated organization information
