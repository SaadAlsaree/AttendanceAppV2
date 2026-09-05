namespace Application.Features.Organizations.Sites.GetById;

public sealed class SiteDetailsResponse
{
    public Guid Id { get; set; }
    public string SiteName { get; set; } = string.Empty;
    public string SiteCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// The units explicitly assigned to this site. Their child units are NOT part of the site
    /// unless they appear in this list in their own right.
    /// </summary>
    public List<SiteUnitResponse> OrganizationalUnits { get; set; } = new();
}

public sealed class SiteUnitResponse
{
    public Guid Id { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public string UnitCode { get; set; } = string.Empty;
    public string? ParentUnitName { get; set; }
    public int EmployeeCount { get; set; }
}
