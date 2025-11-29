using Application.Abstractions.Messaging;

namespace Application.Organizations.OrganizationalUnits.Update;

public sealed class UpdateOrganizationalUnitCommand : ICommand
{
    public Guid OrganizationalUnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public string UnitCode { get; set; } = string.Empty;
    public string? UnitDescription { get; set; }
    public Guid? ParentUnitId { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public string? UnitLogo { get; set; }
    public int? UnitLevel { get; set; }
    public Guid? ManagerId { get; set; }
}
