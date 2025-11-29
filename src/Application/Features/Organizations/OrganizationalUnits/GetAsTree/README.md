# Get Organizational Units As Tree

This feature retrieves organizational units in a hierarchical tree structure, allowing for better visualization and management of the organizational hierarchy.

## Overview

The `GetAsTree` feature provides a hierarchical view of organizational units, where each unit can have parent and child units, forming a tree structure. This is particularly useful for:

-  Organizational charts
-  Hierarchical reporting
-  Department management
-  Employee assignment visualization

## Components

### 1. GetOrganizationalUnitsAsTreeQuery

-  **Purpose**: Defines the query for retrieving organizational units as a tree
-  **Response**: Returns a list of `OrganizationalUnitTreeResponse` objects representing the root nodes of the tree

### 2. OrganizationalUnitTreeResponse

-  **Purpose**: Response model that includes all organizational unit properties plus tree-specific fields
-  **Key Features**:
   -  `Children`: List of child organizational units
   -  `HasChildren`: Boolean indicating if the unit has child units
   -  `TotalEmployeeCount`: Aggregated count of employees including all child units
   -  `TotalChildUnitCount`: Aggregated count of child units including all nested levels

### 3. GetOrganizationalUnitsAsTreeQueryHandler

-  **Purpose**: Handles the query execution and builds the tree structure
-  **Key Features**:
   -  Fetches all organizational units with their relationships
   -  Builds a hierarchical tree structure from flat data
   -  Calculates aggregated counts for employees and child units
   -  Filters out deleted units

## Tree Structure Algorithm

1. **Data Retrieval**: Fetches all organizational units with their parent, manager, children, and employees
2. **Flat Response Creation**: Converts entities to response objects
3. **Tree Building**:
   -  Creates a lookup dictionary for efficient parent-child mapping
   -  Identifies root units (units without parents)
   -  Builds the tree by adding child units to their parents
4. **Aggregated Counts**: Recursively calculates total employee and child unit counts

## Usage Example

```csharp
var query = new GetOrganizationalUnitsAsTreeQuery();
var result = await queryHandler.Handle(query, cancellationToken);

// result contains root organizational units with their children
foreach (var rootUnit in result.Value)
{
    Console.WriteLine($"Root Unit: {rootUnit.UnitName}");
    Console.WriteLine($"Total Employees: {rootUnit.TotalEmployeeCount}");
    Console.WriteLine($"Total Child Units: {rootUnit.TotalChildUnitCount}");

    foreach (var child in rootUnit.Children)
    {
        Console.WriteLine($"  Child: {child.UnitName}");
    }
}
```

## Response Structure

```json
[
  {
    "id": "guid",
    "unitName": "Headquarters",
    "unitCode": "HQ",
    "unitDescription": "Main office",
    "parentUnitId": null,
    "parentUnitName": null,
    "email": "hq@company.com",
    "phoneNumber": "+1234567890",
    "address": "123 Main St",
    "postalCode": "12345",
    "unitLogo": "/logos/hq.png",
    "unitLevel": 1,
    "managerId": "manager-guid",
    "managerName": "John Doe",
    "employeeCount": 50,
    "childUnitCount": 3,
    "createdAt": "2024-01-01T00:00:00Z",
    "updatedAt": "2024-01-01T00:00:00Z",
    "children": [
      {
        "id": "child-guid",
        "unitName": "IT Department",
        "unitCode": "IT",
        "employeeCount": 20,
        "childUnitCount": 2,
        "totalEmployeeCount": 25,
        "totalChildUnitCount": 3,
        "children": [...]
      }
    ],
    "hasChildren": true,
    "totalEmployeeCount": 75,
    "totalChildUnitCount": 6
  }
]
```

## Performance Considerations

-  The feature loads all organizational units in a single query with includes
-  Tree building is done in memory for better performance
-  Aggregated counts are calculated recursively
-  Consider implementing pagination for very large organizational structures

## Dependencies

-  `Application.Abstractions.Data.IApplicationDbContext`
-  `Application.Abstractions.Messaging.IQueryHandler`
-  `Microsoft.EntityFrameworkCore`
-  `SharedKernel.Result`
