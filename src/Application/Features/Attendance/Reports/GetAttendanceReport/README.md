# Get Attendance Report

## Description

Generates comprehensive attendance reports with various metrics, statistics, and analytics for management and HR purposes.

## Operations

-  **Query**: `GetAttendanceReportQuery`
-  **Handler**: `GetAttendanceReportQueryHandler`
-  **Response**: `AttendanceReportResponse`

## Business Rules

-  User must have report generation permissions
-  Date range must be valid and reasonable
-  Results can be filtered by organization, department, employee, or team
-  Includes attendance statistics, late arrivals, early departures, and overtime
-  Supports multiple export formats (PDF, Excel, CSV)
-  Data is aggregated based on selected grouping options
-  Real-time data is used for current period reports

## Input Parameters

-  `StartDate` (DateTime) - Report start date
-  `EndDate` (DateTime) - Report end date
-  `OrganizationId` (Guid, optional) - Filter by organization
-  `OrganizationalUnitId` (Guid, optional) - Filter by department/unit
-  `EmployeeId` (Guid, optional) - Filter by specific employee
-  `ManagerId` (Guid, optional) - Filter by manager's team
-  `ReportType` (ReportType enum) - Type of report (Daily, Weekly, Monthly, Custom)
-  `GroupBy` (string, optional) - Grouping option (Employee, Department, Date)
-  `IncludeBreaks` (bool, optional) - Include break time analysis
-  `IncludeOvertime` (bool, optional) - Include overtime analysis
-  `ExportFormat` (ExportFormat enum, optional) - Export format

## Output

-  `AttendanceReportResponse` with comprehensive report data
-  Aggregated statistics and metrics
-  Employee attendance summaries
-  Late arrival and early departure analysis
-  Overtime calculations
-  Export file (if requested)

## Related Entities

-  `Attendance` - Main data source
-  `Employee` - Employee information
-  `OrganizationalUnit` - Department/unit information
-  `AttendanceBreak` - Break time data
-  `WorkLocation` - Location-based analysis
