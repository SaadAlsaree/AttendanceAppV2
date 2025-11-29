# Get Leave Report

## Description

Generates comprehensive leave reports with leave statistics, patterns, and balance analysis for HR and management.

## Operations

-  **Query**: `GetLeaveReportQuery`
-  **Handler**: `GetLeaveReportQueryHandler`
-  **Response**: `LeaveReportResponse`

## Business Rules

-  User must have report generation permissions
-  Date range must be valid and reasonable
-  Results can be filtered by organization, department, employee, or leave type
-  Includes leave balance analysis and utilization statistics
-  Supports multiple export formats (PDF, Excel, CSV)
-  Data is aggregated based on selected grouping options
-  Leave patterns and trends are analyzed

## Input Parameters

-  `StartDate` (DateTime) - Report start date
-  `EndDate` (DateTime) - Report end date
-  `OrganizationId` (Guid, optional) - Filter by organization
-  `OrganizationalUnitId` (Guid, optional) - Filter by department/unit
-  `EmployeeId` (Guid, optional) - Filter by specific employee
-  `LeaveType` (LeaveType enum, optional) - Filter by leave type
-  `Status` (LeaveStatus enum, optional) - Filter by leave status
-  `ReportType` (ReportType enum) - Type of report (Daily, Weekly, Monthly, Custom)
-  `GroupBy` (string, optional) - Grouping option (Employee, Department, LeaveType)
-  `IncludeBalance` (bool, optional) - Include leave balance analysis
-  `ExportFormat` (ExportFormat enum, optional) - Export format

## Output

-  `LeaveReportResponse` with comprehensive leave data
-  Leave utilization statistics
-  Leave balance analysis
-  Leave pattern analysis
-  Approval/rejection statistics
-  Export file (if requested)

## Related Entities

-  `Leave` - Main data source
-  `Employee` - Employee information
-  `OrganizationalUnit` - Department/unit information
