# Update Attendance Record

## Description

Updates an existing attendance record's information including times, locations, and status.

## Operations

-  **Command**: `UpdateAttendanceCommand`
-  **Handler**: `UpdateAttendanceCommandHandler`
-  **Validator**: `UpdateAttendanceCommandValidator`

## Business Rules

-  Attendance record must exist in the system
-  Cannot update approved attendance records without proper authorization
-  Check-in and check-out times must be logical (check-in before check-out)
-  Location information can be updated
-  Notes can be modified
-  Status can be updated based on business rules

## Input Parameters

-  `AttendanceId` (Guid) - Unique identifier of the attendance record to update
-  `CheckInTime` (DateTime, optional) - Updated check-in time
-  `CheckOutTime` (DateTime, optional) - Updated check-out time
-  `CheckInLocation` (string, optional) - Updated check-in location
-  `CheckOutLocation` (string, optional) - Updated check-out location
-  `CheckInLatitude` (double, optional) - Updated check-in latitude
-  `CheckInLongitude` (double, optional) - Updated check-in longitude
-  `CheckOutLatitude` (double, optional) - Updated check-out latitude
-  `CheckOutLongitude` (double, optional) - Updated check-out longitude
-  `CheckInWorkLocationId` (Guid, optional) - Updated check-in work location
-  `CheckOutWorkLocationId` (Guid, optional) - Updated check-out work location
-  `Notes` (string, optional) - Updated notes
-  `Status` (AttendanceStatus, optional) - Updated status

## Output

-  `AttendanceResponse` with updated attendance details
-  Success/Error result with appropriate messages

## Related Entities

-  `Attendance` - Main entity being updated
-  `WorkLocation` - Associated work locations
-  `Employee` - Associated employee
