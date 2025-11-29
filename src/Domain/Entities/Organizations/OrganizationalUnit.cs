using Domain.Common;
using Domain.Entities.Users;

namespace Domain.Entities.Organizations;

public sealed class OrganizationalUnit : AuditableEntity<Guid>
{
    public string UnitName { get; set; } = string.Empty;
    public string UnitCode { get; set; } = string.Empty; // e.g., "HR", "IT", "Finance"
    public string? UnitDescription { get; set; }
    public Guid? ParentUnitId { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public string? UnitLogo { get; set; } // Path to logo image
    public int? UnitLevel { get; set; }
    public Guid? ManagerId { get; set; }

    // Navigation Properties
    public OrganizationalUnit? ParentUnit { get; set; }
    public Employee? Manager { get; set; }
    public List<OrganizationalUnit> ChildUnits { get; set; } = new List<OrganizationalUnit>();
    public List<Employee> Employees { get; set; } = new List<Employee>();
    public List<User> Users { get; set; } = new List<User>();

}
