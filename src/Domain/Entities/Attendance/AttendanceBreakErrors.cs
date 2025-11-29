using SharedKernel;

namespace Domain.Entities.Attendance;

public static class AttendanceBreakErrors
{
    public static Error NotFound(Guid breakId) => Error.NotFound(
        "AttendanceBreak.NotFound",
        $"The attendance break with Id = '{breakId}' was not found");

    public static Error AttendanceNotFound(Guid attendanceId) => Error.NotFound(
        "AttendanceBreak.AttendanceNotFound",
        $"The attendance record with Id = '{attendanceId}' was not found");

    public static Error InvalidBreakTime(DateTime startTime, DateTime endTime) => Error.Problem(
        "AttendanceBreak.InvalidBreakTime",
        $"Invalid break time: start time '{startTime}' must be before end time '{endTime}'");

    public static Error BreakTimeInFuture(DateTime startTime) => Error.Problem(
        "AttendanceBreak.BreakTimeInFuture",
        $"Break start time '{startTime}' cannot be in the future");

    public static Error BreakOutsideAttendancePeriod(DateTime startTime, DateTime attendanceDate) => Error.Problem(
        "AttendanceBreak.BreakOutsideAttendancePeriod",
        $"Break start time '{startTime}' is outside the attendance period for date '{attendanceDate}'");

    public static Error OverlappingBreaks(Guid attendanceId, DateTime startTime, DateTime endTime) => Error.Conflict(
        "AttendanceBreak.OverlappingBreaks",
        $"Break time period ({startTime} - {endTime}) overlaps with existing breaks for attendance '{attendanceId}'");

    public static Error InvalidBreakDuration(int durationMinutes) => Error.Problem(
        "AttendanceBreak.InvalidBreakDuration",
        $"Invalid break duration: '{durationMinutes}' minutes. Duration must be between 1 and 480 minutes");

    public static Error BreakAlreadyEnded(Guid breakId) => Error.Problem(
        "AttendanceBreak.BreakAlreadyEnded",
        $"The attendance break with Id = '{breakId}' has already ended");

    public static Error CannotModifyEndedBreak(Guid breakId) => Error.Problem(
        "AttendanceBreak.CannotModifyEndedBreak",
        $"Cannot modify the attendance break with Id = '{breakId}' as it has already ended");

    public static Error InvalidBreakType(string breakType) => Error.Problem(
        "AttendanceBreak.InvalidBreakType",
        $"Invalid break type: '{breakType}'");
}
