using SharedKernel;

namespace Domain.Entities.Attendance;

public static class ScheduleDayErrors
{
    public static Error NotFound(Guid scheduleDayId) => Error.NotFound(
        "ScheduleDay.NotFound",
        $"The schedule day with Id = '{scheduleDayId}' was not found");

    public static Error InvalidScheduleDay(Guid scheduleDayId) => Error.Problem(
        "ScheduleDay.InvalidScheduleDay",
        $"The schedule day with Id = '{scheduleDayId}' is invalid or does not belong to the specified schedule");

    public static Error ShiftNotFound(Guid shiftId) => Error.NotFound(
        "ScheduleDay.ShiftNotFound",
        $"The shift with Id = '{shiftId}' was not found");

    public static Error InvalidScheduleDayDate(DateOnly scheduleDayDate) => Error.Problem(
        "ScheduleDay.InvalidScheduleDayDate",
        $"Invalid schedule day date: '{scheduleDayDate}'");

    public static Error DuplicateScheduleDayDate(Guid scheduleId, DateOnly scheduleDayDate) => Error.Conflict(
        "ScheduleDay.DuplicateScheduleDayDate",
        $"A schedule day for date '{scheduleDayDate}' already exists in schedule '{scheduleId}'");
}
