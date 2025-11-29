# Get Attendance Schedule By ID

## Description

Retrieves a specific attendance schedule by its unique identifier with complete details and relationships.

## Operations

-  **Query**: `GetAttendanceScheduleByIdQuery`
-  **Handler**: `GetAttendanceScheduleByIdQueryHandler`
-  **Response**: `AttendanceScheduleResponse`

## Business Rules

-  Attendance schedule must exist in the system
-  Returns complete schedule information including relationships
-  Includes organization details
-  Includes working days and times
-  Includes break configuration
-  Includes overtime and grace period settings

## Input Parameters

-  `AttendanceScheduleId` (Guid) - Unique identifier of the attendance schedule to retrieve

## Output

-  `AttendanceScheduleResponse` with complete schedule details and relationships
-  Error result if attendance schedule is not found

## Related Entities

-  `AttendanceSchedule` - Main entity being queried
-  `OrganizationalUnit` - Associated organization information
