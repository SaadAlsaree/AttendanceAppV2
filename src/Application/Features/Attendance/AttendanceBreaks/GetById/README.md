# Get Attendance Break By ID

## Description

Retrieves a specific attendance break by its unique identifier with complete details and relationships.

## Operations

-  **Query**: `GetAttendanceBreakByIdQuery`
-  **Handler**: `GetAttendanceBreakByIdQueryHandler`
-  **Response**: `AttendanceBreakResponse`

## Business Rules

-  Attendance break must exist in the system
-  Returns complete break information including relationships
-  Includes employee details
-  Includes attendance record information
-  Includes break type and duration details

## Input Parameters

-  `AttendanceBreakId` (Guid) - Unique identifier of the attendance break to retrieve

## Output

-  `AttendanceBreakResponse` with complete break details and relationships
-  Error result if attendance break is not found

## Related Entities

-  `AttendanceBreak` - Main entity being queried
-  `Employee` - Associated employee information
-  `Attendance` - Associated attendance record information
