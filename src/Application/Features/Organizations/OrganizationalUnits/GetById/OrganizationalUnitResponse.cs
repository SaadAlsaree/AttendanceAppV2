namespace Application.Organizations.OrganizationalUnits.GetById;

public sealed class OrganizationalUnitResponse
{
    public Guid Id { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public string UnitCode { get; set; } = string.Empty;
    public string? UnitDescription { get; set; }
    public Guid? ParentUnitId { get; set; }
    public string? ParentUnitName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public string? UnitLogo { get; set; }
    public int? UnitLevel { get; set; }
    public Guid? ManagerId { get; set; }
    public string? ManagerName { get; set; }
    public int EmployeeCount { get; set; }
    public int ChildUnitCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
