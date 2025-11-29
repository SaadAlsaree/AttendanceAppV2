using Domain.Common;

namespace Domain.Entities.Organizations;

public sealed class Holiday : AuditableEntity<Guid>
{
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public bool IsRecurring { get; set; }

    // Navigation Properties
    public OrganizationalUnit Organization { get; set; } = null!;

}
