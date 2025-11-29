# Create Leave Request

## Description

Creates a new leave request for an employee with approval workflow and leave balance validation.

## Operations

-  **Command**: `CreateLeaveCommand`
-  **Handler**: `CreateLeaveCommandHandler`
-  **Validator**: `CreateLeaveCommandValidator`

## Business Rules

-  Employee must exist and be active
-  Leave type must be valid (Annual, Sick, Personal, etc.)
-  Start date cannot be in the past
-  End date must be after or equal to start date
-  Leave duration must be within policy limits
-  Employee must have sufficient leave balance
-  Cannot create overlapping leave requests
-  Leave status is initially set to "Pending"
-  Manager approval is required for most leave types

## Input Parameters

-  `EmployeeId` (Guid) - ID of the employee requesting leave
-  `LeaveType` (LeaveType enum) - Type of leave (Annual, Sick, Personal, etc.)
-  `StartDate` (DateTime) - Leave start date
-  `EndDate` (DateTime) - Leave end date
-  `DurationDays` (int) - Number of leave days
-  `Reason` (string) - Reason for leave request
-  `ManagerId` (Guid, optional) - ID of the manager for approval
-  `EmergencyContact` (string, optional) - Emergency contact information
-  `Notes` (string, optional) - Additional notes

## Output

-  `LeaveResponse` with created leave details
-  Success/Error result with appropriate messages

## Related Entities

-  `Leave` - Main entity being created
-  `Employee` - Associated employee
-  `Employee` - Manager for approval
