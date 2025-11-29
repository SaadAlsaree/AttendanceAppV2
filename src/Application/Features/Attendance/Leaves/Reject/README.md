# Reject Leave Request

## Description

Rejects a leave request by an authorized manager, providing a reason for the rejection.

## Operations

-  **Command**: `RejectLeaveCommand`
-  **Handler**: `RejectLeaveCommandHandler`
-  **Validator**: `RejectLeaveCommandValidator`

## Business Rules

-  Leave request must exist in the system
-  User must have rejection permissions
-  Leave request must be in "Pending" status
-  Rejection timestamp is recorded
-  Rejector information is recorded
-  Leave status is updated to "Rejected"
-  Rejection reason must be provided
-  Employee is notified of rejection

## Input Parameters

-  `LeaveId` (Guid) - ID of the leave request to reject
-  `RejectedBy` (Guid) - ID of the user rejecting the leave
-  `RejectionReason` (string) - Reason for rejection
-  `RejectionNotes` (string, optional) - Additional notes from the rejector

## Output

-  `LeaveResponse` with updated leave details
-  Success/Error result with appropriate messages

## Related Entities

-  `Leave` - Main entity being updated
-  `User` - Rejector user
-  `Employee` - Associated employee
