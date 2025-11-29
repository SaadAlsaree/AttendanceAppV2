# Get Attendance Log By ID

## Description

Retrieves a specific attendance log by its unique identifier with complete details and relationships from biometric devices.

## Operations

-  **Query**: `GetAttendanceLogByIdQuery`
-  **Handler**: `GetAttendanceLogByIdQueryHandler`
-  **Response**: `AttendanceLogResponse`

## Business Rules

-  Attendance log must exist in the system
-  Returns complete log information including relationships
-  Includes employee details
-  Includes organization information
-  Includes attendance record details
-  Includes device information (card reader, door number, etc.)
-  Includes verification mode and status

## Input Parameters

-  `AttendanceLogId` (Guid) - Unique identifier of the attendance log to retrieve

## Output

-  `AttendanceLogResponse` with complete log details and relationships
-  Error result if attendance log is not found

## Related Entities

-  `AttendanceLog` - Main entity being queried
-  `Employee` - Associated employee information
-  `OrganizationalUnit` - Associated organization information
-  `Attendance` - Associated attendance record information
