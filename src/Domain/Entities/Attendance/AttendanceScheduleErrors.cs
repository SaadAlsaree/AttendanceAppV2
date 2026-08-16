using SharedKernel;

namespace Domain.Entities.Attendance;

public static class AttendanceScheduleErrors
{
    public static Error NotFound(Guid scheduleId) => Error.NotFound(
        "AttendanceSchedule.NotFound",
        $"The attendance schedule with Id = '{scheduleId}' was not found");

    public static Error InvalidDateRange(DateTime startDate, DateTime endDate) => Error.Problem(
        "AttendanceSchedule.InvalidDateRange",
        $"Invalid date range: start date '{startDate}' must be before end date '{endDate}'");

    public static Error ScheduleAlreadyExists(Guid employeeId, DateTime date) => Error.Conflict(
        "AttendanceSchedule.ScheduleAlreadyExists",
        $"An attendance schedule already exists for employee '{employeeId}' on date '{date}'");

    public static Error InactiveSchedule(Guid scheduleId) => Error.Problem(
        "AttendanceSchedule.InactiveSchedule",
        $"The attendance schedule with Id = '{scheduleId}' is inactive");

    public static Error ShiftNotFound() => Error.Problem(
        "AttendanceSchedule.ShiftNotFound",
        "One or more of the referenced shifts were not found");
}
