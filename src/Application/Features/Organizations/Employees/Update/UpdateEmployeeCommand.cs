using Application.Abstractions.Messaging;
using Domain.Enums;

namespace Application.Features.Organizations.Employees.Update;

public sealed class UpdateEmployeeCommand : ICommand
{
    public Guid Id { get; set; }
    public string EmpId { get; set; } = string.Empty;
    public string RFID { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string SecondName { get; set; } = string.Empty;
    public string ThirdName { get; set; } = string.Empty;
    public string FourthName { get; set; } = string.Empty;
    public string FamilyName { get; set; } = string.Empty;
    public Guid OrganizationalUnitId { get; set; }
    public Guid? ManagerId { get; set; }
    public bool IsManager { get; set; }
    public Role Role { get; set; }
}
