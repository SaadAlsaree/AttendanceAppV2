namespace Application.Attendance.Shared;

public interface IAttendanceCalculationService
{
    AttendanceMetrics CalculateMetrics(
        DateTime checkInTime,
        DateTime checkOutTime,
        Domain.Entities.Organizations.Shift shift);

    // NEW: Calculate metrics with hourly leave consideration
    AttendanceMetrics CalculateMetricsWithLeave(
        DateTime checkInTime,
        DateTime checkOutTime,
        Domain.Entities.Organizations.Shift shift,
        IEnumerable<Domain.Entities.Attendance.AttendanceBreak> approvedLeaves);
}

public record AttendanceMetrics(
    int WorkingMinutes,
    int LateMinutes,
    int EarlyLeaveMinutes,
    int OvertimeMinutes);
