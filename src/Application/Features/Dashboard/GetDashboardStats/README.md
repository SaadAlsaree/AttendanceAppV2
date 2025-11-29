# Dashboard Statistics

## Description

Retrieves comprehensive dashboard statistics and metrics for the attendance management system, providing insights across multiple domains including employees, attendance, devices, leaves, and performance.

## Operations

-  **Query**: `GetDashboardStatsQuery`
-  **Handler**: `GetDashboardStatsQueryHandler`
-  **Validator**: `GetDashboardStatsQueryValidator`

## Business Rules

-  Organization ID is required
-  Date range validation (start date <= end date)
-  Statistics are calculated based on the specified date range
-  Department and employee filtering is optional
-  Real-time data aggregation from multiple entities
-  Performance metrics include attendance rates, punctuality, and productivity scores

## Input Parameters

-  `OrganizationId` (Guid) - ID of the organization
-  `StartDate` (DateTime?, optional) - Start date for statistics (defaults to 30 days ago)
-  `EndDate` (DateTime?, optional) - End date for statistics (defaults to today)
-  `DepartmentId` (Guid?, optional) - Filter by specific department
-  `EmployeeId` (Guid?, optional) - Filter by specific employee

## Output

### Overall Statistics

-  Total employees count
-  Active employees count
-  Today's attendance breakdown (present, absent, late, on leave, remote work)

### Attendance Statistics

-  Check-in/check-out counts
-  Pending approvals count
-  Verified/rejected logs count
-  Average working hours, overtime, late minutes, early leave minutes
-  Department-wise attendance statistics
-  Daily, weekly, and monthly attendance trends

### Device Statistics

-  Total, online, offline, active, inactive devices
-  Devices with issues count
-  System uptime percentage
-  Detailed device status information

### Leave Statistics

-  Total leave requests
-  Pending, approved, rejected leave counts
-  Employees currently on leave
-  Average leave days
-  Leave type breakdown with approval rates

### Performance Metrics

-  Overall attendance rate
-  Punctuality rate
-  Productivity score
-  Employee satisfaction score
-  System uptime
-  Data accuracy rate
-  Top performing employees

### Recent Activities

-  Latest attendance log activities
-  Employee actions and timestamps
-  Activity status and location information

### Alerts

-  Device offline alerts
-  Pending approval notifications
-  High absenteeism warnings
-  System health alerts

## Related Entities

-  `Employee` - Employee information and statistics
-  `Attendance` - Attendance records and calculations
-  `AttendanceLog` - Log entries and verification status
-  `Device` - Device status and health monitoring
-  `Leave` - Leave requests and approvals
-  `OrganizationalUnit` - Department structure and statistics

## Performance Considerations

-  Uses efficient Entity Framework queries with proper indexing
-  Implements pagination for large datasets
-  Caches frequently accessed statistics
-  Optimized database queries with proper joins
-  Real-time data aggregation with minimal performance impact

## Security

-  Requires proper authorization
-  Organization-level data isolation
-  Department-level access control
-  Employee-level privacy protection
-  Audit trail for all dashboard access

## Usage Examples

### Get overall dashboard statistics

```csharp
var query = new GetDashboardStatsQuery(organizationId);
var result = await handler.Handle(query, cancellationToken);
```

### Get department-specific statistics

```csharp
var query = new GetDashboardStatsQuery(organizationId, departmentId: departmentId);
var result = await handler.Handle(query, cancellationToken);
```

### Get date range statistics

```csharp
var query = new GetDashboardStatsQuery(
    organizationId,
    startDate: DateTime.Today.AddDays(-7),
    endDate: DateTime.Today);
var result = await handler.Handle(query, cancellationToken);
```
