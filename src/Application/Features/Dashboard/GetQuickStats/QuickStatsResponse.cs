namespace Application.Features.Dashboard.GetQuickStats;

public sealed class QuickStatsResponse
{
    public DateTime Date { get; set; }
    public int TotalEmployees { get; set; }
    public int PresentToday { get; set; }
    public int AbsentToday { get; set; }
    public int LateToday { get; set; }
    public int OnLeaveToday { get; set; }
    public double AttendanceRate { get; set; }
    public int PendingApprovals { get; set; }
    public int OnlineDevices { get; set; }
    public int OfflineDevices { get; set; }
    public int ActiveAlerts { get; set; }
    public double AverageWorkingHours { get; set; }
    public double AverageOvertimeHours { get; set; }
    public int EmployeesOnLeave { get; set; }
    public int RecentActivities { get; set; }
}
