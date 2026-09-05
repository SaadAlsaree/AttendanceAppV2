using Domain.Enums;

namespace Application.Models;

public class UserInfoDto
{
    public Guid Id { get; set; }
    public string Username { get; set; }
    public string UserLogin { get; set; }
    public Role Role { get; set; }
    public string RoleName { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime LastLoginDate { get; set; }
    public UserStatus Status { get; set; }
    public Guid? OrganizationalUnitId { get; set; }
    public string? OrganizationalUnitName { get; set; }
    public Guid? SiteId { get; set; }
    public string? SiteName { get; set; }
}
