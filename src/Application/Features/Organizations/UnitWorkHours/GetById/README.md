# Get Unit Work Hours By ID

## Description

Retrieves a specific unit work hours configuration by its unique identifier with complete details.

## Operations

-  **Query**: `GetUnitWorkHoursByIdQuery`
-  **Handler**: `GetUnitWorkHoursByIdQueryHandler`
-  **Response**: `UnitWorkHoursResponse`

## Business Rules

-  Unit work hours must exist in the system
-  Returns complete work hours information including relationships
-  Includes organizational unit details
-  Includes working hours and break configuration
-  Includes overtime and grace period settings

## Input Parameters

-  `UnitWorkHoursId` (Guid) - Unique identifier of the unit work hours to retrieve

## Output

-  `UnitWorkHoursResponse` with complete work hours details and relationships
-  Error result if unit work hours is not found

## Related Entities

-  `UnitWorkHours` - Main entity being queried
-  `OrganizationalUnit` - Associated organizational unit information
