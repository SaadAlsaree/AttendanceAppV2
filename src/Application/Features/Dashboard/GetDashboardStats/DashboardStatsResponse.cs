using Domain.Enums;

namespace Application.Features.Dashboard.GetDashboardStats;

public sealed class DashboardStatsResponse
{
    // Overall Statistics
    public int TotalEmployees { get; set; }
    public int ActiveEmployees { get; set; }
    public int PresentToday { get; set; }
    public int AbsentToday { get; set; }
    public int LateToday { get; set; }
    public int OnLeaveToday { get; set; }
    public int RemoteWorkToday { get; set; }

    // Attendance Statistics
    public AttendanceStats AttendanceStats { get; set; } = new();
    public AttendanceTrends AttendanceTrends { get; set; } = new();
    public List<DepartmentAttendanceStats> DepartmentStats { get; set; } = new();

    // Device Statistics
    public DeviceStats DeviceStats { get; set; } = new();
    public List<DeviceStatusInfo> DeviceStatuses { get; set; } = new();

    // Leave Statistics
    public LeaveStats LeaveStats { get; set; } = new();
    public List<LeaveTypeStats> LeaveTypeStats { get; set; } = new();

    // Performance Metrics
    public PerformanceMetrics PerformanceMetrics { get; set; } = new();
    public List<EmployeePerformance> TopPerformers { get; set; } = new();

    // Recent Activities
    public List<RecentActivity> RecentActivities { get; set; } = new();
    public List<AlertItem> Alerts { get; set; } = new();
}

public sealed class AttendanceStats
{
    public int TotalCheckIns { get; set; }
    public int TotalCheckOuts { get; set; }
    public int PendingApprovals { get; set; }
    public int VerifiedLogs { get; set; }
    public int RejectedLogs { get; set; }
    public double AverageWorkingHours { get; set; }
    public double AverageOvertimeHours { get; set; }
    public double AverageLateMinutes { get; set; }
    public double AverageEarlyLeaveMinutes { get; set; }
}

public sealed class AttendanceTrends
{
    public List<DailyTrend> DailyTrends { get; set; } = new();
    public List<WeeklyTrend> WeeklyTrends { get; set; } = new();
    public List<MonthlyTrend> MonthlyTrends { get; set; } = new();
}

public sealed class DailyTrend
{
    public DateTime Date { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public int LateCount { get; set; }
    public double AverageWorkingHours { get; set; }
    public double AverageOvertimeHours { get; set; }
}

public sealed class WeeklyTrend
{
    public DateTime WeekStart { get; set; }
    public DateTime WeekEnd { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public double AverageWorkingHours { get; set; }
    public double AverageOvertimeHours { get; set; }
}

public sealed class MonthlyTrend
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public double AverageWorkingHours { get; set; }
    public double AverageOvertimeHours { get; set; }
}

public sealed class DepartmentAttendanceStats
{
    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public int TotalEmployees { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public int LateCount { get; set; }
    public double AttendanceRate { get; set; }
    public double AverageWorkingHours { get; set; }
}

public sealed class DeviceStats
{
    public int TotalDevices { get; set; }
    public int OnlineDevices { get; set; }
    public int OfflineDevices { get; set; }
    public int ActiveDevices { get; set; }
    public int InactiveDevices { get; set; }
    public int DevicesWithIssues { get; set; }
    public double UptimePercentage { get; set; }
}

public sealed class DeviceStatusInfo
{
    public Guid DeviceId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DeviceStatus Status { get; set; }
    public bool IsOnline { get; set; }
    public DateTime? LastSync { get; set; }
    public string IpAddress { get; set; } = string.Empty;
}

public sealed class LeaveStats
{
    public int TotalLeaveRequests { get; set; }
    public int PendingApprovals { get; set; }
    public int ApprovedLeaves { get; set; }
    public int RejectedLeaves { get; set; }
    public int EmployeesOnLeave { get; set; }
    public double AverageLeaveDays { get; set; }
}

public sealed class LeaveTypeStats
{
    public LeaveType LeaveType { get; set; }
    public string LeaveTypeName { get; set; } = string.Empty;
    public int RequestCount { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }
    public double ApprovalRate { get; set; }
}

public sealed class PerformanceMetrics
{
    public double OverallAttendanceRate { get; set; }
    public double PunctualityRate { get; set; }
    public double ProductivityScore { get; set; }
    public double EmployeeSatisfactionScore { get; set; }
    public double SystemUptime { get; set; }
    public double DataAccuracyRate { get; set; }
}

public sealed class EmployeePerformance
{
    public Guid EmployeeId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public double AttendanceRate { get; set; }
    public double PunctualityRate { get; set; }
    public double AverageWorkingHours { get; set; }
    public double OvertimeHours { get; set; }
    public int LateCount { get; set; }
    public int AbsentCount { get; set; }
}

public sealed class RecentActivity
{
    public Guid Id { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Location { get; set; }
}

public sealed class AlertItem
{
    public Guid Id { get; set; }
    public string AlertType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsResolved { get; set; }
    public DateTime? ResolvedAt { get; set; }
}
