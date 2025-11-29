# Get Absence Report

## Description

Generates comprehensive absence reports with detailed analysis of employee absences, patterns, and trends for HR and management purposes.

## Operations

-  **Query**: `GetAbsenceReportQuery`
-  **Handler**: `GetAbsenceReportQueryHandler`
-  **Response**: `AbsenceReportResponse`

## Business Rules

-  User must have report generation permissions
-  Date range must be valid and reasonable
-  Results can be filtered by organization, department, employee, or absence type
-  Includes absence statistics, patterns, and trend analysis
-  Supports multiple export formats (PDF, Excel, CSV)
-  Data is aggregated based on selected grouping options
-  Absence patterns and trends are analyzed
-  Minimum absence days threshold can be applied

## Input Parameters

-  `StartDate` (DateTime) - Report start date
-  `EndDate` (DateTime) - Report end date
-  `OrganizationId` (Guid, optional) - Filter by organization
-  `OrganizationalUnitId` (Guid, optional) - Filter by department/unit
-  `EmployeeId` (Guid, optional) - Filter by specific employee
-  `AbsenceType` (AttendanceStatus, optional) - Filter by absence type
-  `ReportType` (ReportType enum) - Type of report (Daily, Weekly, Monthly, Custom)
-  `GroupBy` (string, optional) - Grouping option (Employee, Department, Date)
-  `IncludePatterns` (bool, optional) - Include absence pattern analysis
-  `MinAbsenceDays` (int, optional) - Minimum absence days threshold
-  `ExportFormat` (ExportFormat enum, optional) - Export format

## Output

-  `AbsenceReportResponse` with comprehensive absence data
-  Absence statistics and metrics
-  Employee absence summaries
-  Department absence summaries
-  Absence pattern analysis
-  Most frequent absence day calculation
-  Export file (if requested)

## Related Entities

-  `Attendance` - Main data source for absence calculations
-  `Employee` - Employee information
-  `OrganizationalUnit` - Department/unit information
-  `AttendanceSchedule` - Schedule information for absence calculations
