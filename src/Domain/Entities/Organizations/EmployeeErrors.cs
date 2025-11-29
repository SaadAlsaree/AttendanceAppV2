using SharedKernel;

namespace Domain.Entities.Organizations;

public static class EmployeeErrors
{
    public static Error NotFound(Guid Id) => Error.NotFound(
        "Employee.NotFound",
        $"The employee with Id = '{Id}' was not found");

    public static Error NotFound(string employeeNumber) => Error.NotFound(
        "Employee.NotFound",
        $"The employee with number = '{employeeNumber}' was not found");

    public static Error DuplicateEmployeeNumber(string employeeNumber) => Error.Conflict(
        "Employee.DuplicateEmployeeNumber",
        $"An employee with number = '{employeeNumber}' already exists");

    public static Error DuplicateEmail(string email) => Error.Conflict(
        "Employee.DuplicateEmail",
        $"An employee with email = '{email}' already exists");

    public static Error InvalidManagerAssignment(Guid employeeId, Guid managerId) => Error.Problem(
        "Employee.InvalidManagerAssignment",
        $"Cannot assign employee '{employeeId}' as manager to themselves");

    public static Error InactiveEmployee(Guid employeeId) => Error.Problem(
        "Employee.InactiveEmployee",
        $"The employee with Id = '{employeeId}' is inactive");

    public static Error InvalidHireDate(DateTime hireDate) => Error.Problem(
        "Employee.InvalidHireDate",
        $"Hire date '{hireDate}' cannot be in the future");

    public static Error InvalidBirthDate(DateTime birthDate) => Error.Problem(
        "Employee.InvalidBirthDate",
        $"Birth date '{birthDate}' is invalid");

    public static Error InvalidProfileImage() => Error.Problem(
        "Employee.InvalidProfileImage",
        $"Profile image is invalid");

    public static Error InvalidFaceImage() => Error.Problem(
        "Employee.InvalidFaceImage",
        $"Face image is invalid");

    public static Error InvalidNationalIdFront() => Error.Problem(
        "Employee.InvalidNationalIdFront",
        $"National ID front is invalid");

    public static Error InvalidNationalIdBack() => Error.Problem(
        "Employee.InvalidNationalIdBack",
        $"National ID back is invalid");

    public static Error DuplicateEmployeeCheckIn(Guid employeeId) => Error.Conflict(
        "Employee.DuplicateEmployeeCheckIn",
        $"الموظف صاحب المعرف = '{employeeId}' لقد قام بالفعل بالتسجيل اليوم");

}
