# Check In Employee

## Description

Records an employee's check-in time and device information from biometric devices for a specific attendance record.

## Operations

-  **Command**: `CheckInCommand`
-  **Handler**: `CheckInCommandHandler`
-  **Validator**: `CheckInCommandValidator`

## Business Rules

-  Attendance record must exist for the employee on the given date
-  Employee cannot check in multiple times on the same day
-  Check-in time cannot be in the future
-  Device information is recorded from biometric devices
-  Check-in method is recorded (device, mobile app, web, etc.)
-  Attendance status is updated to "Present"
-  Late minutes are calculated if applicable

## Input Parameters

-  `EmployeeId` (int) - ID of the employee
-  `CheckInTime` (DateTime) - Check-in timestamp
-  `Major` (int, optional) - Major device identifier
-  `Minor` (int, optional) - Minor device identifier
-  `CardNo` (string) - Card number used for authentication
-  `CardType` (int, optional) - Type of card used
-  `Name` (string) - Employee name from device
-  `CardReaderNo` (int, optional) - Card reader number
-  `DoorNo` (int, optional) - Door number
-  `EmployeeNoString` (string, optional) - Employee number as string
-  `SerialNo` (int, optional) - Serial number
-  `UserType` (string, optional) - Type of user
-  `CurrentVerifyMode` (string, optional) - Current verification mode
-  `AttendanceStatus` (string, optional) - Attendance status
-  `Label` (string, optional) - Label information
-  `Mask` (string, optional) - Mask information
-  `PictureURL` (string, optional) - URL to captured image
-  `Notes` (string, optional) - Additional notes

## Output

-  `AttendanceResponse` with updated attendance details
-  Success/Error result with appropriate messages

## Related Entities

-  `Attendance` - Main entity being updated
-  `Employee` - Associated employee
-  `AttendanceLog` - Log entry created for check-in
-  `AttendanceCheckedInDomainEvent` - Domain event raised on successful check-in
