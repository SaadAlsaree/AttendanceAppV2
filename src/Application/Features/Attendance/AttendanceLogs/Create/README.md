# Create Attendance Log

## Description

Creates a new attendance log entry recording an employee's attendance event with detailed information from biometric devices.

## Operations

-  **Command**: `CreateAttendanceLogCommand`
-  **Handler**: `CreateAttendanceLogCommandHandler`
-  **Validator**: `CreateAttendanceLogCommandValidator`

## Business Rules

-  Employee must exist and be active
-  Time cannot be in the future
-  All required fields must be provided
-  Card number must be unique for the employee
-  Device information must be valid

## Input Parameters

-  `EmployeeId` (int) - ID of the employee
-  `OrganizationId` (Guid, optional) - ID of the organization
-  `AttendanceId` (Guid, optional) - ID of the attendance record
-  `Major` (int) - Major device identifier
-  `Minor` (int) - Minor device identifier
-  `Time` (DateTime) - Log timestamp
-  `CardNo` (string) - Card number used for authentication
-  `CardType` (int) - Type of card used
-  `Name` (string) - Employee name from device
-  `CardReaderNo` (int) - Card reader number
-  `DoorNo` (int) - Door number
-  `EmployeeNoString` (string) - Employee number as string
-  `SerialNo` (int) - Serial number
-  `UserType` (string) - Type of user
-  `CurrentVerifyMode` (string) - Current verification mode
-  `AttendanceStatus` (string) - Attendance status
-  `Label` (string) - Label information
-  `Mask` (string) - Mask information
-  `PictureURL` (string) - URL to captured image

## Output

-  `Guid` - ID of the created attendance log
-  Success/Error result with appropriate messages

## Related Entities

-  `AttendanceLog` - Main entity being created
-  `Employee` - Associated employee
-  `OrganizationalUnit` - Associated organization
-  `Attendance` - Associated attendance record (if applicable)
-  `AttendanceLogCreatedDomainEvent` - Domain event raised on successful creation
