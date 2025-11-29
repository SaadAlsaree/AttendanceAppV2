using SharedKernel;

namespace Domain.Entities.Attendance;

public static class AttendanceLogErrors
{
    public static Error NotFound(Guid logId) => Error.NotFound(
        "AttendanceLog.NotFound",
        $"The attendance log with Id = '{logId}' was not found");

    public static Error AlreadyRejected(Guid logId) => Error.Problem(
        "AttendanceLog.AlreadyRejected",
        $"The attendance log with Id = '{logId}' is already rejected");

    public static Error AlreadyVerified(Guid logId) => Error.Problem(
        "AttendanceLog.AlreadyVerified",
        $"The attendance log with Id = '{logId}' is already verified");

    public static Error InvalidLogType(string logType) => Error.Problem(
        "AttendanceLog.InvalidLogType",
        $"Invalid log type: '{logType}'");

    public static Error InvalidTimestamp(DateTime timestamp) => Error.Problem(
        "AttendanceLog.InvalidTimestamp",
        $"Invalid timestamp: '{timestamp}' cannot be in the future");
}
