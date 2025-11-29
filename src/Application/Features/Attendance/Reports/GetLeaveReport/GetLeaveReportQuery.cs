using Application.Abstractions.Messaging;
using Domain.Enums;

namespace Application.Attendance.Reports.GetLeaveReport;

public sealed class GetLeaveReportQuery : IQuery<LeaveReportResponse>
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? OrganizationalUnitId { get; set; }
    public Guid? EmployeeId { get; set; }
    public LeaveType? LeaveType { get; set; }
    public LeaveStatus? LeaveStatus { get; set; }
    public ReportType ReportType { get; set; }
    public string? GroupBy { get; set; }
    public bool IncludeApprovalDetails { get; set; } = true;
    public ExportFormat? ExportFormat { get; set; }
}

public sealed class LeaveReportResponse
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public ReportType ReportType { get; set; }
    public LeaveStatistics Statistics { get; set; } = new();
    public List<EmployeeLeaveSummary> EmployeeSummaries { get; set; } = new();
    public List<DepartmentLeaveSummary> DepartmentSummaries { get; set; } = new();
    public string? ExportFileUrl { get; set; }
    public DateTime GeneratedAt { get; set; }
}

public sealed class LeaveStatistics
{
    public int TotalLeaveRequests { get; set; }
    public int ApprovedLeaves { get; set; }
    public int PendingLeaves { get; set; }
    public int RejectedLeaves { get; set; }
    public int CancelledLeaves { get; set; }
    public decimal TotalLeaveDays { get; set; }
    public decimal AverageLeaveDuration { get; set; }
    public decimal ApprovalRate { get; set; }
    public Dictionary<LeaveType, int> LeaveTypeDistribution { get; set; } = new();
}

public sealed class EmployeeLeaveSummary
{
    public Guid EmployeeId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public int TotalLeaveRequests { get; set; }
    public int ApprovedLeaves { get; set; }
    public int PendingLeaves { get; set; }
    public int RejectedLeaves { get; set; }
    public decimal TotalLeaveDays { get; set; }
    public decimal AverageLeaveDuration { get; set; }
    public Dictionary<LeaveType, int> LeaveTypeCount { get; set; } = new();
    public decimal ApprovalRate { get; set; }
}

public sealed class DepartmentLeaveSummary
{
    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public int TotalEmployees { get; set; }
    public int EmployeesWithLeaves { get; set; }
    public int TotalLeaveRequests { get; set; }
    public int ApprovedLeaves { get; set; }
    public int PendingLeaves { get; set; }
    public int RejectedLeaves { get; set; }
    public decimal TotalLeaveDays { get; set; }
    public decimal AverageLeaveDuration { get; set; }
    public decimal ApprovalRate { get; set; }
}
