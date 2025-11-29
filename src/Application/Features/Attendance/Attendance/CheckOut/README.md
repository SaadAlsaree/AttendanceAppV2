# Check Out Employee

## Description

Records an employee's check-out time and device information from biometric devices for a specific attendance record.

## Operations

-  **Command**: `CheckOutCommand`
-  **Handler**: `CheckOutCommandHandler`
-  **Validator**: `CheckOutCommandValidator`

## Business Rules

-  Attendance record must exist for the employee on the given date
-  Employee must have already checked in
-  Employee cannot check out multiple times on the same day
-  Check-out time cannot be before check-in time
-  Check-out time cannot be in the future
-  Device information is recorded from biometric devices
-  Check-out method is recorded
-  Working minutes, overtime, and early leave minutes are calculated
-  Attendance status is updated based on metrics

## Input Parameters

-  `EmployeeId` (int) - ID of the employee
-  `AttendanceId` (Guid) - ID of the attendance record
-  `CheckOutTime` (DateTime) - Check-out timestamp
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
-  `AttendanceLog` - Log entry created for check-out
-  `AttendanceCheckedOutDomainEvent` - Domain event raised on successful check-out
