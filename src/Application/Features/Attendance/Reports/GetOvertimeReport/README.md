# Get Overtime Report

## Description

Generates overtime reports with detailed analysis of employee overtime hours, costs, and patterns for payroll and management.

## Operations

-  **Query**: `GetOvertimeReportQuery`
-  **Handler**: `GetOvertimeReportQueryHandler`
-  **Response**: `OvertimeReportResponse`

## Business Rules

-  User must have report generation permissions
-  Date range must be valid and reasonable
-  Results can be filtered by organization, department, employee, or overtime type
-  Includes overtime cost calculations and budget analysis
-  Supports multiple export formats (PDF, Excel, CSV)
-  Data is aggregated based on selected grouping options
-  Overtime patterns and trends are analyzed

## Input Parameters

-  `StartDate` (DateTime) - Report start date
-  `EndDate` (DateTime) - Report end date
-  `OrganizationId` (Guid, optional) - Filter by organization
-  `OrganizationalUnitId` (Guid, optional) - Filter by department/unit
-  `EmployeeId` (Guid, optional) - Filter by specific employee
-  `OvertimeType` (string, optional) - Filter by overtime type (Regular, Holiday, Weekend)
-  `ReportType` (ReportType enum) - Type of report (Daily, Weekly, Monthly, Custom)
-  `GroupBy` (string, optional) - Grouping option (Employee, Department, Date)
-  `IncludeCosts` (bool, optional) - Include overtime cost calculations
-  `MinOvertimeHours` (int, optional) - Minimum overtime hours threshold
-  `ExportFormat` (ExportFormat enum, optional) - Export format

## Output

-  `OvertimeReportResponse` with comprehensive overtime data
-  Overtime hours statistics
-  Cost analysis and budget impact
-  Overtime pattern analysis
-  Employee overtime summaries
-  Export file (if requested)

## Related Entities

-  `Attendance` - Main data source for overtime calculations
-  `Employee` - Employee information
-  `OrganizationalUnit` - Department/unit information
-  `AttendanceSchedule` - Schedule information for overtime calculations
