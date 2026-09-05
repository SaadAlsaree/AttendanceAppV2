using Application.Abstractions.Messaging;

namespace Application.Features.Organizations.Sites.SetUnits;

/// <summary>
/// Replaces the site's unit membership with exactly <paramref name="OrganizationalUnitIds"/>.
/// <para>
/// Membership is explicit and non-transitive: only the listed units join the site. Their child
/// units do not, unless they are listed themselves.
/// </para>
/// </summary>
public sealed record SetSiteUnitsCommand(Guid SiteId, List<Guid> OrganizationalUnitIds) : ICommand;
