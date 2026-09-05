using Application.Abstractions.Messaging;

namespace Application.Features.Organizations.Sites.Create;

public sealed class CreateSiteCommand : ICommand<Guid>
{
    public string SiteName { get; set; } = string.Empty;
    public string SiteCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
}
