namespace Application.Organizations.OrganizationalUnits.GetAsTree;

public sealed class OrganizationalUnitTreeResponse
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

    // Tree-specific properties
    public List<OrganizationalUnitTreeResponse> Children { get; set; } = new List<OrganizationalUnitTreeResponse>();
    public bool HasChildren => Children.Any();
    public int TotalEmployeeCount { get; set; } // Includes employees from all child units
    public int TotalChildUnitCount { get; set; } // Includes all nested child units
}
