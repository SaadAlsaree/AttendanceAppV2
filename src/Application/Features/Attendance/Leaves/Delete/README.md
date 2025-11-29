# Delete Leave Request

## Description

Deletes a leave request from the system. This operation may be soft delete or hard delete based on business requirements.

## Operations

-  **Command**: `DeleteLeaveCommand`
-  **Handler**: `DeleteLeaveCommandHandler`
-  **Validator**: `DeleteLeaveCommandValidator`

## Business Rules

-  Leave request must exist in the system
-  Cannot delete approved leave requests without proper authorization
-  Cannot delete leave requests that have already started
-  System may perform soft delete instead of hard delete
-  Deletion is logged for audit purposes
-  Leave balance may need to be adjusted

## Input Parameters

-  `LeaveId` (Guid) - Unique identifier of the leave request to delete

## Output

-  Success/Error result with appropriate messages
-  Confirmation of deletion operation

## Related Entities

-  `Leave` - Main entity being deleted
-  `Employee` - Associated employee (leave balance may be affected)
