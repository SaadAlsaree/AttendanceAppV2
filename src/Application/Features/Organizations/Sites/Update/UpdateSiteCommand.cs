using Application.Abstractions.Messaging;

namespace Application.Features.Organizations.Sites.Update;

public sealed class UpdateSiteCommand : ICommand
{
    public Guid Id { get; set; }
    public string SiteName { get; set; } = string.Empty;
    public string SiteCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
}
