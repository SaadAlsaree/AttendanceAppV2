# Update Leave Request

## Description

Updates an existing leave request's information including dates, duration, and reason.

## Operations

-  **Command**: `UpdateLeaveCommand`
-  **Handler**: `UpdateLeaveCommandHandler`
-  **Validator**: `UpdateLeaveCommandValidator`

## Business Rules

-  Leave request must exist in the system
-  Cannot update approved or rejected leave requests
-  Start date cannot be in the past
-  End date must be after or equal to start date
-  Leave duration must be within policy limits
-  Employee must have sufficient leave balance
-  Cannot create overlapping leave requests
-  Only pending leave requests can be updated

## Input Parameters

-  `LeaveId` (Guid) - Unique identifier of the leave request to update
-  `LeaveType` (LeaveType enum, optional) - Updated leave type
-  `StartDate` (DateTime, optional) - Updated start date
-  `EndDate` (DateTime, optional) - Updated end date
-  `DurationDays` (int, optional) - Updated number of leave days
-  `Reason` (string, optional) - Updated reason for leave
-  `EmergencyContact` (string, optional) - Updated emergency contact
-  `Notes` (string, optional) - Updated notes

## Output

-  `LeaveResponse` with updated leave details
-  Success/Error result with appropriate messages

## Related Entities

-  `Leave` - Main entity being updated
-  `Employee` - Associated employee
