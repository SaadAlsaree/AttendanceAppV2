namespace Application.Features.Organizations.Sites.Get;

public sealed class SiteResponse
{
    public Guid Id { get; set; }
    public string SiteName { get; set; } = string.Empty;
    public string SiteCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; }
    public int UnitCount { get; set; }
    public int UserCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
