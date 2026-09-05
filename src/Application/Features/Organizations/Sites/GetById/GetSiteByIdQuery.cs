using Application.Abstractions.Messaging;

namespace Application.Features.Organizations.Sites.GetById;

public sealed record GetSiteByIdQuery(Guid Id) : IQuery<SiteDetailsResponse>;
