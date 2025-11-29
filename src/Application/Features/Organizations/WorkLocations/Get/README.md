# Get Work Locations

## Description

Retrieves all work locations for a specific organizational unit, ordered by name.

## Operations

-  **Query**: `GetWorkLocationsQuery`
-  **Handler**: `GetWorkLocationsQueryHandler`
-  **Response**: `WorkLocationResponse`

## Business Rules

-  Only returns work locations for the specified organizational unit
-  Results are ordered alphabetically by name
-  Organizational unit must exist
-  Returns all work locations regardless of active status

## Input Parameters

-  `OrganizationId` (Guid) - ID of the organizational unit to get work locations for

## Output

-  `List<WorkLocationResponse>` - List of work locations with complete details

## Response Properties

-  `Id` (Guid) - Unique identifier
-  `OrganizationId` (Guid) - Associated organizational unit ID
-  `Name` (string) - Work location name
-  `Address` (string) - Physical address
-  `Latitude` (double) - GPS latitude coordinate
-  `Longitude` (double) - GPS longitude coordinate
-  `RadiusMeters` (int) - Radius in meters for verification
-  `IsActive` (bool) - Whether the location is active
-  `Description` (string, optional) - Additional description
-  `WifiSSID` (string, optional) - WiFi network name
-  `BeaconId` (string, optional) - Beacon identifier
-  `CreatedAt` (DateTime) - Creation timestamp
-  `UpdatedAt` (DateTime, optional) - Last update timestamp

## Related Entities

-  `WorkLocation` - Main entity being queried
-  `OrganizationalUnit` - Associated organizational unit information
