using SharedKernel;

namespace Domain.Entities.Organizations;

public static class OrganizationErrors
{
    public static class Department
    {
        public static readonly Error NotFound = Error.NotFound(
            "Department.NotFound",
            "The department with the specified identifier was not found");

        public static readonly Error CodeAlreadyExists = Error.Conflict(
            "Department.CodeAlreadyExists",
            "The department code already exists");

        public static readonly Error HasDirectorates = Error.Conflict(
            "Department.HasDirectorates",
            "Cannot delete department that has directorates");
    }

    public static class Directorate
    {
        public static readonly Error NotFound = Error.NotFound(
            "Directorate.NotFound",
            "The directorate with the specified identifier was not found");

        public static readonly Error CodeAlreadyExists = Error.Conflict(
            "Directorate.CodeAlreadyExists",
            "The directorate code already exists");

        public static readonly Error HasSections = Error.Conflict(
            "Directorate.HasSections",
            "Cannot delete directorate that has sections");
    }

    public static class Section
    {
        public static readonly Error NotFound = Error.NotFound(
            "Section.NotFound",
            "The section with the specified identifier was not found");

        public static readonly Error CodeAlreadyExists = Error.Conflict(
            "Section.CodeAlreadyExists",
            "The section code already exists");

        public static readonly Error HasEmployees = Error.Conflict(
            "Section.HasEmployees",
            "Cannot delete section that has employees");
    }

    public static class Employee
    {
        public static Error NotFound(Guid employeeId) => Error.NotFound(
            "Employee.NotFound",
            $"The employee with the Id = '{employeeId}' was not found");

        public static readonly Error NotFoundByEmail = Error.NotFound(
            "Employee.NotFoundByEmail",
            "The employee with the specified email was not found");

        public static readonly Error EmailNotUnique = Error.Conflict(
            "Employee.EmailNotUnique",
            "The provided email is not unique");

        public static Error Unauthorized() => Error.Failure(
            "Employee.Unauthorized",
            "You are not authorized to perform this action.");

        public static readonly Error EmployeeNumberAlreadyExists = Error.Conflict(
            "Employee.EmployeeNumberAlreadyExists",
            "The employee number already exists");

        public static readonly Error UserAlreadyEmployee = Error.Conflict(
            "Employee.UserAlreadyEmployee",
            "The user is already an employee");

        public static readonly Error InvalidManager = Error.Failure(
            "Employee.InvalidManager",
            "The specified manager is not valid");

        public static readonly Error CannotAssignToSelf = Error.Failure(
            "Employee.CannotAssignToSelf",
            "Cannot assign employee as their own manager");
    }

    public static class OrganizationalUnit
    {
        public static Error NotFound(Guid unitId) => Error.NotFound(
            "OrganizationalUnit.NotFound",
            $"The organizational unit with the Id = '{unitId}' was not found");

        public static Error UnitCodeAlreadyExists(string unitCode) => Error.Conflict(
            "OrganizationalUnit.UnitCodeAlreadyExists",
            $"The unit code '{unitCode}' already exists");

        public static Error ParentUnitNotFound(Guid parentUnitId) => Error.NotFound(
            "OrganizationalUnit.ParentUnitNotFound",
            $"The parent unit with the Id = '{parentUnitId}' was not found");

        public static readonly Error HasChildUnits = Error.Conflict(
            "OrganizationalUnit.HasChildUnits",
            "Cannot delete organizational unit that has child units");

        public static readonly Error HasEmployees = Error.Conflict(
            "OrganizationalUnit.HasEmployees",
            "Cannot delete organizational unit that has employees");
    }

    public static class Holiday
    {
        public static Error NotFound(Guid holidayId) => Error.NotFound(
            "Holiday.NotFound",
            $"The holiday with the Id = '{holidayId}' was not found");

        public static Error DateAlreadyExists(DateOnly date) => Error.Conflict(
            "Holiday.DateAlreadyExists",
            $"A holiday already exists for the date {date:yyyy-MM-dd}");

        public static readonly Error CannotDeletePastHoliday = Error.Failure(
            "Holiday.CannotDeletePastHoliday",
            "Cannot delete a holiday that has already passed");

        public static readonly Error CannotUpdatePastHoliday = Error.Failure(
            "Holiday.CannotUpdatePastHoliday",
            "Cannot update a holiday that has already passed");
    }
}
