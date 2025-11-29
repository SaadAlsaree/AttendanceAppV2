namespace Application.Features.Organizations.Employees.Profile;

public sealed record ProfileResponse(
    Guid Id,
    string EmployeeId,
    string Code,
    string RFID,
    string FirstName,
    string SecondName,
    string? ThirdName,
    string? FourthName,
    string FamilyName,
    string FullName,
    string Email,
    Guid? ManagerId,
    string? ManagerName,
    Guid? OrganizationalUnitId,
    string? OrganizationalUnitName,
    string? ProfileImage,
    DateTime CreatedAt);
