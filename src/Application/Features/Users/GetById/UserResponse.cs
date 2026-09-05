using Domain.Enums;

namespace Application.Features.Users.GetById;

public sealed class UserResponse
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string UserLogin { get; set; } = string.Empty;
    public Role Role { get; set; }
    public UserStatus Status { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginDate { get; set; }
    public Guid? OrganizationalUnitId { get; set; }
    public string? OrganizationalUnitName { get; set; }
    public string? OrganizationalUnitCode { get; set; }
    public Guid? SiteId { get; set; }
    public string? SiteName { get; set; }
}
