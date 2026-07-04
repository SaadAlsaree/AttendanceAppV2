using Domain.Entities.Attendance;

namespace Application.Attendance.Shared;

public enum ShiftSource
{
    None = 0,
    ScheduleException = 1,
    ScheduleDay = 2,
    WeeklyPattern = 3
}

public readonly record struct ResolvedShift(Guid? ShiftId, Guid? AttendanceScheduleId, ShiftSource Source);

/// <summary>
/// Single source of truth for "which shift applies to this employee on this date".
/// Precedence: schedule exception (ScheduleIssue) → schedule day → fixed weekly pattern → none.
/// A schedule whose ExcludedDates contains the date is an explicit day off and beats the pattern.
/// </summary>
public static class ShiftResolution
{
    public static ResolvedShift Resolve(
        DateOnly date,
        IReadOnlyList<AttendanceSchedule> activeSchedulesCoveringDate,
        Func<Guid, ScheduleIssue?> exceptionForDate,
        Func<Guid, ScheduleDay?> scheduleDayForDate,
        Guid? weeklyPatternShiftId)
    {
        if (activeSchedulesCoveringDate.Count > 0)
        {
            AttendanceSchedule schedule = activeSchedulesCoveringDate
                .FirstOrDefault(s => !s.ExcludedDates.Contains(date));

            if (schedule is null)
            {
                // Every covering schedule excludes this date: explicit day off.
                return new ResolvedShift(null, null, ShiftSource.None);
            }

            ScheduleIssue exception = exceptionForDate(schedule.Id);
            if (exception is not null)
            {
                return new ResolvedShift(exception.ShiftId, schedule.Id, ShiftSource.ScheduleException);
            }

            ScheduleDay scheduleDay = scheduleDayForDate(schedule.Id);
            if (scheduleDay is not null)
            {
                return new ResolvedShift(scheduleDay.ShiftId, schedule.Id, ShiftSource.ScheduleDay);
            }

            // Schedule covers the date but defines no day for it — fall back to the weekly pattern.
        }

        if (weeklyPatternShiftId.HasValue)
        {
            return new ResolvedShift(weeklyPatternShiftId, null, ShiftSource.WeeklyPattern);
        }

        return new ResolvedShift(null, null, ShiftSource.None);
    }
}
