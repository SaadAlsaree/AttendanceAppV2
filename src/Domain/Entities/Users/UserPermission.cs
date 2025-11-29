using System.ComponentModel.DataAnnotations.Schema;
using Domain.Common;

namespace Domain.Entities.Users;

public class UserPermission : AuditableEntity<Guid>
{
    public Guid UserId { get; set; }
    public Guid PermissionId { get; set; }

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public User User { get; set; }

    [ForeignKey(nameof(PermissionId))]
    public Permission Permission { get; set; }

    public DateTime? ExpiryDate { get; set; }
    public bool IsActive { get; set; } = true;
}
