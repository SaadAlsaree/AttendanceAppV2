using Application.Abstractions.Messaging;

namespace Application.Organizations.OrganizationalUnits.Get;

public sealed record GetOrganizationalUnitsQuery(
    string? SearchText = null,
    Guid? ParentUnitId = null) : IQuery<List<OrganizationalUnitResponse>>;
