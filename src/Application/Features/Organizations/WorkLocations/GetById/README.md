# Get Work Location by Id

## Description

Retrieves a specific work location by its unique identifier.

## Operations

-  **Query**: `GetWorkLocationByIdQuery`
-  **Handler**: `GetWorkLocationByIdQueryHandler`
-  **Response**: `WorkLocationResponse`

## Business Rules

-  Returns the work location if it exists
-  Returns a not found error if the work location doesn't exist
-  No additional filtering or authorization checks

## Input Parameters

-  `WorkLocationId` (Guid) - Unique identifier of the work location

## Output

-  `WorkLocationResponse` - Complete work location details
-  Error if work location not found

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
