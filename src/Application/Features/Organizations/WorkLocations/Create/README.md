# Create Work Location

## Description

Creates a new work location for an organizational unit with GPS coordinates, radius, and optional verification methods.

## Operations

-  **Command**: `CreateWorkLocationCommand`
-  **Handler**: `CreateWorkLocationCommandHandler`
-  **Validator**: `CreateWorkLocationCommandValidator`

## Business Rules

-  Work location must belong to an existing organizational unit
-  Work location name must be unique within the organization
-  GPS coordinates must be valid (latitude: -90 to 90, longitude: -180 to 180)
-  Radius must be between 1 and 10,000 meters
-  Optional WiFi SSID and Beacon ID for additional verification methods

## Input Parameters

-  `OrganizationId` (Guid) - ID of the organizational unit
-  `Name` (string) - Name of the work location (max 255 characters)
-  `Address` (string) - Physical address (max 500 characters)
-  `Latitude` (double) - GPS latitude coordinate (-90 to 90)
-  `Longitude` (double) - GPS longitude coordinate (-180 to 180)
-  `RadiusMeters` (int) - Radius in meters for location verification (1-10000)
-  `IsActive` (bool) - Whether the work location is active
-  `Description` (string, optional) - Additional description (max 1000 characters)
-  `WifiSSID` (string, optional) - WiFi network name for verification (max 100 characters)
-  `BeaconId` (string, optional) - Beacon identifier for indoor positioning (max 100 characters)

## Output

-  `Guid` - ID of the created work location

## Related Entities

-  `WorkLocation` - Main entity being created
-  `OrganizationalUnit` - Associated organizational unit
-  `WorkLocationCreatedDomainEvent` - Domain event raised on creation

## Validation Rules

-  Organization ID must not be empty
-  Name must not be empty and maximum 255 characters
-  Address must not be empty and maximum 500 characters
-  Latitude must be between -90 and 90
-  Longitude must be between -180 and 180
-  Radius must be between 1 and 10,000 meters
-  Optional fields have length limits when provided
