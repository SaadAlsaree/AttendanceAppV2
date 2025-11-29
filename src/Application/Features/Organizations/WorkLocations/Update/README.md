# Update Work Location

## Description

Updates an existing work location with new information while maintaining data integrity.

## Operations

-  **Command**: `UpdateWorkLocationCommand`
-  **Handler**: `UpdateWorkLocationCommandHandler`
-  **Validator**: `UpdateWorkLocationCommandValidator`

## Business Rules

-  Work location must exist in the system
-  Work location name must be unique within the organization (excluding current work location)
-  GPS coordinates must be valid (latitude: -90 to 90, longitude: -180 to 180)
-  Radius must be between 1 and 10,000 meters
-  Updates the `UpdatedAt` timestamp automatically
-  Raises domain event for tracking changes

## Input Parameters

-  `Id` (Guid) - ID of the work location to update
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

-  Success/Error result with appropriate messages

## Related Entities

-  `WorkLocation` - Main entity being updated
-  `WorkLocationUpdatedDomainEvent` - Domain event raised on update

## Validation Rules

-  ID must not be empty
-  Name must not be empty and maximum 255 characters
-  Address must not be empty and maximum 500 characters
-  Latitude must be between -90 and 90
-  Longitude must be between -180 and 180
-  Radius must be between 1 and 10,000 meters
-  Optional fields have length limits when provided
