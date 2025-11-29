# Approve Attendance Record

## Description

Approves an attendance record by an authorized manager or administrator.

## Operations

-  **Command**: `ApproveAttendanceCommand`
-  **Handler**: `ApproveAttendanceCommandHandler`
-  **Validator**: `ApproveAttendanceCommandValidator`

## Business Rules

-  Attendance record must exist in the system
-  User must have approval permissions
-  Attendance record must not already be approved
-  Attendance record must have both check-in and check-out times
-  Approver information is recorded
-  Approval timestamp is recorded
-  Attendance status is updated to "Approved"

## Input Parameters

-  `AttendanceId` (Guid) - ID of the attendance record to approve
-  `ApprovedBy` (Guid) - ID of the user approving the attendance
-  `ApprovalNotes` (string, optional) - Notes from the approver

## Output

-  `AttendanceResponse` with updated attendance details
-  Success/Error result with appropriate messages

## Related Entities

-  `Attendance` - Main entity being updated
-  `User` - Approver user
-  `Employee` - Associated employee
-  `AttendanceApprovedDomainEvent` - Domain event raised on successful approval
