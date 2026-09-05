using Domain.Common;
using Domain.Entities.Users;

namespace Domain.Entities.Organizations;

/// <summary>
/// An administrative grouping of organizational units — a "site" (الموقع).
/// <para>
/// Membership is <b>explicit and non-transitive</b>: assigning a unit to a site does NOT bring its
/// child units along. A site is therefore a flat, possibly scattered set of units drawn from
/// anywhere in the org tree, which is what distinguishes it from the subtree scoping used by
/// <see cref="Domain.Enums.Role.OrgSupervisor"/>.
/// </para>
/// <para>
/// This is not <see cref="WorkLocation"/>. That type is a geofence (lat/long/radius) used to verify
/// where a check-in physically happened; this one carries no geography and exists only to scope
/// access and reporting.
/// </para>
/// </summary>
public sealed class Site : AuditableEntity<Guid>
{
    public string SiteName { get; set; } = string.Empty;
    public string SiteCode { get; set; } = string.Empty; // e.g., "NORTH", "HQ"
    public string? Description { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation Properties
    public List<OrganizationalUnit> OrganizationalUnits { get; set; } = new();
    public List<User> Users { get; set; } = new();
}
