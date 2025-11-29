using Application.Abstractions.Messaging;

namespace Application.Organizations.OrganizationalUnits.Get;

public sealed record GetOrganizationalUnitsQuery : IQuery<List<OrganizationalUnitResponse>>;
