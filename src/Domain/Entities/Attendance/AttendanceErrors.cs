using SharedKernel;

namespace Domain.Entities.Attendance;

public static class AttendanceErrors
{
    public static Error NotFound(Guid attendanceId) => Error.NotFound(
        "Attendance.NotFound",
        $"The attendance record with Id = '{attendanceId}' was not found");

    public static Error AlreadyCheckedIn(Guid employeeId) => Error.Problem(
        "Attendance.AlreadyCheckedIn",
        $"The attendance record with Id = '{employeeId}' is already checked in");

    public static Error AlreadyCheckedOut(Guid attendanceId) => Error.Problem(
        "Attendance.AlreadyCheckedOut",
        $"The attendance record with Id = '{attendanceId}' is already checked out");

    public static Error NotCheckedIn(Guid attendanceId) => Error.Problem(
        "Attendance.NotCheckedIn",
        $"Cannot check out attendance record with Id = '{attendanceId}' - employee has not checked in");

    public static Error LocationVerificationFailed(Guid attendanceId) => Error.Problem(
        "Attendance.LocationVerificationFailed",
        $"Location verification failed for attendance record with Id = '{attendanceId}'");

    public static Error BiometricVerificationFailed(Guid attendanceId) => Error.Problem(
        "Attendance.BiometricVerificationFailed",
        $"Biometric verification failed for attendance record with Id = '{attendanceId}'");

    public static Error InvalidDateRange(DateTime startDate, DateTime endDate) => Error.Problem(
        "Attendance.InvalidDateRange",
        $"Invalid date range: start date '{startDate}' must be before end date '{endDate}'");

    public static Error AlreadyApproved(Guid attendanceId) => Error.Problem(
        "Attendance.AlreadyApproved",
        $"The attendance record with Id = '{attendanceId}' is already approved");

    public static Error HasRelatedBreaks(Guid attendanceId) => Error.Problem(
        "Attendance.HasRelatedBreaks",
        $"Cannot delete attendance record with Id = '{attendanceId}' - it has related attendance breaks");

    public static Error HasRelatedLogs(Guid attendanceId) => Error.Problem(
        "Attendance.HasRelatedLogs",
        $"Cannot delete attendance record with Id = '{attendanceId}' - it has related attendance logs");

    public static Error MissingScheduleOrShift(Guid employeeId, DateOnly date) => Error.Problem(
        "Attendance.MissingScheduleOrShift",
        $"No active attendance schedule or shift found for employee with Id = '{employeeId}' on date '{date}'.");
}
