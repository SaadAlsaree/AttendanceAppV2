# Update Attendance Log

## Description

Updates an existing attendance log's information from biometric devices including card details, device information, and status.

## Operations

-  **Command**: `UpdateAttendanceLogCommand`
-  **Handler**: `UpdateAttendanceLogCommandHandler`
-  **Validator**: `UpdateAttendanceLogCommandValidator`

## Business Rules

-  Attendance log must exist in the system
-  Cannot update core device information (Major, Minor, Time) after creation
-  Card information can be updated
-  Device details can be modified
-  Attendance status can be updated
-  Label and mask information can be updated
-  Picture URL can be updated

## Input Parameters

-  `AttendanceLogId` (Guid) - Unique identifier of the attendance log to update
-  `CardNo` (string, optional) - Updated card number
-  `CardType` (int, optional) - Updated card type
-  `Name` (string, optional) - Updated employee name from device
-  `CardReaderNo` (int, optional) - Updated card reader number
-  `DoorNo` (int, optional) - Updated door number
-  `EmployeeNoString` (string, optional) - Updated employee number as string
-  `SerialNo` (int, optional) - Updated serial number
-  `UserType` (string, optional) - Updated user type
-  `CurrentVerifyMode` (string, optional) - Updated verification mode
-  `AttendanceStatus` (string, optional) - Updated attendance status
-  `Label` (string, optional) - Updated label information
-  `Mask` (string, optional) - Updated mask information
-  `PictureURL` (string, optional) - Updated picture URL

## Output

-  `AttendanceLogResponse` with updated log details
-  Success/Error result with appropriate messages

## Related Entities

-  `AttendanceLog` - Main entity being updated
-  `Employee` - Associated employee
-  `Attendance` - Associated attendance record
