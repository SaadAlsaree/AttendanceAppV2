# Get Leave Request By ID

## Description

Retrieves a specific leave request by its unique identifier with complete details and relationships.

## Operations

-  **Query**: `GetLeaveByIdQuery`
-  **Handler**: `GetLeaveByIdQueryHandler`
-  **Response**: `LeaveResponse`

## Business Rules

-  Leave request must exist in the system
-  Returns complete leave information including relationships
-  Includes employee details
-  Includes manager information
-  Includes approval/rejection details
-  User must have permission to view the leave request

## Input Parameters

-  `LeaveId` (Guid) - Unique identifier of the leave request to retrieve

## Output

-  `LeaveResponse` with complete leave details and relationships
-  Error result if leave request is not found

## Related Entities

-  `Leave` - Main entity being queried
-  `Employee` - Associated employee information
-  `Employee` - Manager information
