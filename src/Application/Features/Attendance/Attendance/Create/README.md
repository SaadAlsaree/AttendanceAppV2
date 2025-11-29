# Create Attendance Record

## Description

Creates a new attendance record for an employee on a specific date with initial status and schedule information.

## Operations

-  **Command**: `CreateAttendanceCommand`
-  **Handler**: `CreateAttendanceCommandHandler`
-  **Validator**: `CreateAttendanceCommandValidator`

## Business Rules

-  Employee must exist and be active
-  Attendance record must not already exist for the employee on the given date
-  Date cannot be in the future
-  Employee must be assigned to a shift (if shift-based attendance)
-  Attendance schedule must be valid for the employee
-  Initial status is typically set to "Scheduled" or "Present"

## Input Parameters

-  `EmployeeId` (Guid) - ID of the employee
-  `OrganizationId` (Guid) - ID of the organization
-  `Date` (DateTime) - Date of attendance (date only)
-  `ShiftId` (Guid, optional) - ID of the assigned shift
-  `AttendanceScheduleId` (Guid, optional) - ID of the attendance schedule
-  `Notes` (string, optional) - Additional notes

## Output

-  `AttendanceResponse` with created attendance details
-  Success/Error result with appropriate messages

## Related Entities

-  `Attendance` - Main entity being created
-  `Employee` - Associated employee
-  `Shift` - Associated shift (if applicable)
-  `AttendanceSchedule` - Associated schedule (if applicable)
-  `AttendanceCreatedDomainEvent` - Domain event raised on successful creation
