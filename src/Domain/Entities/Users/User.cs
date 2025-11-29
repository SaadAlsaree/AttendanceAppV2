using Domain.Common;
using Domain.Entities.Organizations;
using Domain.Enums;

namespace Domain.Entities.Users;

public sealed class User : AuditableEntity<Guid>
{
    public string Username { get; set; }
    public string UserLogin { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public Role Role { get; set; } = Role.User;
    // Navigation property for many-to-many relationship with Permission through UserPermission
    public ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
    public bool IsActive { get; set; } = true;
    public DateTime LastLoginDate { get; set; } = DateTime.Now;
    public UserStatus Status { get; set; } = UserStatus.Active;
    public bool IsDefaultPassword { get; set; } = true;

    public Guid? OrganizationalUnitId { get; set; }
    public OrganizationalUnit? OrganizationalUnit { get; set; }


}
