using Application.Abstractions.Messaging;

namespace Application.Features.Organizations.Sites.Get;

public sealed record GetSitesQuery(
    string? SearchText = null,
    bool? IsActive = null) : IQuery<List<SiteResponse>>;
