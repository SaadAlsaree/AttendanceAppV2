# Get Late Report

## Description

Generates late arrival reports with detailed analysis of employee tardiness patterns and frequency for management review.

## Operations

-  **Query**: `GetLateReportQuery`
-  **Handler**: `GetLateReportQueryHandler`
-  **Response**: `LateReportResponse`

## Business Rules

-  User must have report generation permissions
-  Date range must be valid and reasonable
-  Results can be filtered by organization, department, employee, or late arrival threshold
-  Includes late arrival frequency analysis and patterns
-  Supports multiple export formats (PDF, Excel, CSV)
-  Data is aggregated based on selected grouping options
-  Late arrival trends and patterns are analyzed

## Input Parameters

-  `StartDate` (DateTime) - Report start date
-  `EndDate` (DateTime) - Report end date
-  `OrganizationId` (Guid, optional) - Filter by organization
-  `OrganizationalUnitId` (Guid, optional) - Filter by department/unit
-  `EmployeeId` (Guid, optional) - Filter by specific employee
-  `MinLateMinutes` (int, optional) - Minimum late minutes threshold
-  `MaxLateMinutes` (int, optional) - Maximum late minutes threshold
-  `ReportType` (ReportType enum) - Type of report (Daily, Weekly, Monthly, Custom)
-  `GroupBy` (string, optional) - Grouping option (Employee, Department, Date)
-  `IncludePatterns` (bool, optional) - Include late arrival pattern analysis
-  `ExportFormat` (ExportFormat enum, optional) - Export format

## Output

-  `LateReportResponse` with comprehensive late arrival data
-  Late arrival statistics and frequency
-  Pattern analysis and trends
-  Employee late arrival summaries
-  Department-wise late arrival analysis
-  Export file (if requested)

## Related Entities

-  `Attendance` - Main data source for late arrival calculations
-  `Employee` - Employee information
-  `OrganizationalUnit` - Department/unit information
-  `AttendanceSchedule` - Schedule information for late arrival calculations
