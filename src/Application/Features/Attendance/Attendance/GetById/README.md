# Get Attendance Record By ID

## Description

Retrieves a specific attendance record by its unique identifier with complete details and relationships, including biometric device logs.

## Operations

-  **Query**: `GetAttendanceByIdQuery`
-  **Handler**: `GetAttendanceByIdQueryHandler`
-  **Response**: `AttendanceResponse`

## Business Rules

-  Attendance record must exist in the system
-  Returns complete attendance information including relationships
-  Includes employee details
-  Includes shift information
-  Includes attendance logs with biometric device data
-  Includes attendance schedule information

## Input Parameters

-  `AttendanceId` (Guid) - Unique identifier of the attendance record to retrieve

## Output

-  `AttendanceResponse` with complete attendance details and relationships
-  Includes `AttendanceLogDto` list with biometric device information:
-  `Major`, `Minor` - Device identifiers
-  `Time` - Log timestamp
-  `CardNo`, `CardType` - Card information
-  `Name` - Employee name from device
-  `CardReaderNo`, `DoorNo` - Device location
-  `EmployeeNoString`, `SerialNo` - Device-specific data
-  `UserType`, `CurrentVerifyMode` - Authentication details
-  `AttendanceStatus` - Status from device
-  `Label`, `Mask`, `PictureURL` - Additional device data
-  Error result if attendance record is not found

## Related Entities

-  `Attendance` - Main entity being queried
-  `Employee` - Associated employee information
-  `Shift` - Associated shift information
-  `AttendanceLog` - Associated biometric device logs
-  `AttendanceSchedule` - Associated schedule information
