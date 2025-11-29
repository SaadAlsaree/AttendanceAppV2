# Approve Leave Request

## Description

Approves a leave request by an authorized manager, updating the leave status and employee leave balance.

## Operations

-  **Command**: `ApproveLeaveCommand`
-  **Handler**: `ApproveLeaveCommandHandler`
-  **Validator**: `ApproveLeaveCommandValidator`

## Business Rules

-  Leave request must exist in the system
-  User must have approval permissions
-  Leave request must be in "Pending" status
-  Employee must have sufficient leave balance
-  Approval timestamp is recorded
-  Approver information is recorded
-  Leave status is updated to "Approved"
-  Employee's leave balance is reduced

## Input Parameters

-  `LeaveId` (Guid) - ID of the leave request to approve
-  `ApprovedBy` (Guid) - ID of the user approving the leave
-  `ApprovalNotes` (string, optional) - Notes from the approver

## Output

-  `LeaveResponse` with updated leave details
-  Success/Error result with appropriate messages

## Related Entities

-  `Leave` - Main entity being updated
-  `User` - Approver user
-  `Employee` - Associated employee (leave balance updated)
