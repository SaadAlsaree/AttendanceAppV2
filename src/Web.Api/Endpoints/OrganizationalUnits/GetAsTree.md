# Get Organizational Units As Tree - API Endpoint

## Overview

This endpoint retrieves organizational units in a hierarchical tree structure, providing a complete view of the organizational hierarchy with aggregated counts.

## Endpoint Details

-  **URL**: `GET /organizational-units/tree`
-  **Method**: GET
-  **Authentication**: Not required (commented out)
-  **Authorization**: Not required (commented out)

## Request

No request parameters required.

## Response

### Success Response (200 OK)

Returns a list of root organizational units with their hierarchical children.

```json
[
   {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "unitName": "Headquarters",
      "unitCode": "HQ",
      "unitDescription": "Main office location",
      "parentUnitId": null,
      "parentUnitName": null,
      "email": "hq@company.com",
      "phoneNumber": "+1234567890",
      "address": "123 Main Street",
      "postalCode": "12345",
      "unitLogo": "/logos/hq.png",
      "unitLevel": 1,
      "managerId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
      "managerName": "John Doe",
      "employeeCount": 50,
      "childUnitCount": 3,
      "createdAt": "2024-01-01T00:00:00Z",
      "updatedAt": "2024-01-01T00:00:00Z",
      "children": [
         {
            "id": "3fa85f64-5717-4562-b3fc-2c963f66afa8",
            "unitName": "IT Department",
            "unitCode": "IT",
            "unitDescription": "Information Technology",
            "parentUnitId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
            "parentUnitName": "Headquarters",
            "email": "it@company.com",
            "phoneNumber": "+1234567891",
            "address": "123 Main Street, IT Wing",
            "postalCode": "12345",
            "unitLogo": "/logos/it.png",
            "unitLevel": 2,
            "managerId": "3fa85f64-5717-4562-b3fc-2c963f66afa9",
            "managerName": "Jane Smith",
            "employeeCount": 20,
            "childUnitCount": 2,
            "createdAt": "2024-01-01T00:00:00Z",
            "updatedAt": "2024-01-01T00:00:00Z",
            "children": [
               {
                  "id": "3fa85f64-5717-4562-b3fc-2c963f66afaa",
                  "unitName": "Software Development",
                  "unitCode": "SD",
                  "unitDescription": "Software Development Team",
                  "parentUnitId": "3fa85f64-5717-4562-b3fc-2c963f66afa8",
                  "parentUnitName": "IT Department",
                  "email": "sd@company.com",
                  "phoneNumber": "+1234567892",
                  "address": "123 Main Street, IT Wing, 2nd Floor",
                  "postalCode": "12345",
                  "unitLogo": "/logos/sd.png",
                  "unitLevel": 3,
                  "managerId": "3fa85f64-5717-4562-b3fc-2c963f66afab",
                  "managerName": "Bob Johnson",
                  "employeeCount": 10,
                  "childUnitCount": 0,
                  "createdAt": "2024-01-01T00:00:00Z",
                  "updatedAt": "2024-01-01T00:00:00Z",
                  "children": [],
                  "hasChildren": false,
                  "totalEmployeeCount": 10,
                  "totalChildUnitCount": 0
               }
            ],
            "hasChildren": true,
            "totalEmployeeCount": 30,
            "totalChildUnitCount": 2
         }
      ],
      "hasChildren": true,
      "totalEmployeeCount": 80,
      "totalChildUnitCount": 5
   }
]
```

### Error Responses

#### 400 Bad Request

```json
{
   "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
   "title": "Bad Request",
   "status": 400,
   "detail": "Invalid request parameters"
}
```

#### 500 Internal Server Error

```json
{
   "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
   "title": "Internal Server Error",
   "status": 500,
   "detail": "An error occurred while processing the request"
}
```

## Response Fields

### OrganizationalUnitTreeResponse

| Field                 | Type                                 | Description                                   |
| --------------------- | ------------------------------------ | --------------------------------------------- |
| `id`                  | Guid                                 | Unique identifier for the organizational unit |
| `unitName`            | string                               | Name of the organizational unit               |
| `unitCode`            | string                               | Short code for the unit (e.g., "IT", "HR")    |
| `unitDescription`     | string?                              | Optional description of the unit              |
| `parentUnitId`        | Guid?                                | ID of the parent unit (null for root units)   |
| `parentUnitName`      | string?                              | Name of the parent unit                       |
| `email`               | string?                              | Email address for the unit                    |
| `phoneNumber`         | string?                              | Phone number for the unit                     |
| `address`             | string?                              | Physical address of the unit                  |
| `postalCode`          | string?                              | Postal code for the unit                      |
| `unitLogo`            | string?                              | Path to the unit's logo image                 |
| `unitLevel`           | int?                                 | Hierarchical level of the unit                |
| `managerId`           | Guid?                                | ID of the unit manager                        |
| `managerName`         | string?                              | Name of the unit manager                      |
| `employeeCount`       | int                                  | Number of employees directly in this unit     |
| `childUnitCount`      | int                                  | Number of direct child units                  |
| `createdAt`           | DateTime                             | When the unit was created                     |
| `updatedAt`           | DateTime?                            | When the unit was last updated                |
| `children`            | List<OrganizationalUnitTreeResponse> | Child organizational units                    |
| `hasChildren`         | bool                                 | Whether the unit has child units              |
| `totalEmployeeCount`  | int                                  | Total employees including all child units     |
| `totalChildUnitCount` | int                                  | Total child units including all nested levels |

## Usage Examples

### cURL

```bash
curl -X GET "https://api.example.com/organizational-units/tree" \
  -H "Accept: application/json"
```

### JavaScript (Fetch API)

```javascript
const response = await fetch('/organizational-units/tree', {
   method: 'GET',
   headers: {
      Accept: 'application/json'
   }
});

const organizationalTree = await response.json();
console.log('Organizational Tree:', organizationalTree);
```

### C# (HttpClient)

```csharp
using var client = new HttpClient();
var response = await client.GetAsync("/organizational-units/tree");
var organizationalTree = await response.Content.ReadFromJsonAsync<List<OrganizationalUnitTreeResponse>>();
```

## Notes

-  The endpoint returns only non-deleted organizational units
-  Root units (units without parents) are returned at the top level
-  Child units are nested within their parent units
-  Aggregated counts include all nested levels
-  The tree structure supports unlimited nesting levels
-  Performance is optimized with a single database query
