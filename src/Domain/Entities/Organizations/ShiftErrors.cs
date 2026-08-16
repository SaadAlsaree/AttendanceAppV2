using SharedKernel;

namespace Domain.Entities.Organizations;

public static class ShiftErrors
{
    public static Error NotFound(Guid id) =>
        new("Shift.NotFound", $"Shift with ID {id} was not found.", ErrorType.NotFound);

    public static Error NameAlreadyExists(string name) =>
        new("Shift.NameAlreadyExists", $"Shift with name '{name}' already exists in this organization.", ErrorType.Conflict);

    public static Error CannotDeleteShiftInUse() =>
        new("Shift.CannotDeleteInUse", "Cannot delete a shift that is currently assigned to employees.", ErrorType.Validation);

    public static Error CannotDeleteShiftWithAttendanceRecords() =>
        new("Shift.CannotDeleteWithAttendanceRecords", "Cannot delete a shift that has associated attendance records.", ErrorType.Validation);

    public static Error CannotUpdateShiftInUse() =>
        new("Shift.CannotUpdateInUse", "Cannot update a shift that is currently assigned to employees.", ErrorType.Validation);

    public static Error InactiveShift(Guid id) =>
        new("Shift.Inactive", $"Shift with ID {id} is inactive and cannot be assigned.", ErrorType.Validation);
}
