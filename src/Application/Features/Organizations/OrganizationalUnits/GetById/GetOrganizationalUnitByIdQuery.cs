using Application.Abstractions.Messaging;

namespace Application.Organizations.OrganizationalUnits.GetById;

public sealed record GetOrganizationalUnitByIdQuery(Guid OrganizationalUnitId) : IQuery<OrganizationalUnitResponse>;
