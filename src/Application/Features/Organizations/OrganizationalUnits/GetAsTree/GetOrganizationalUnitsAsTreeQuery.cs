using Application.Abstractions.Messaging;

namespace Application.Organizations.OrganizationalUnits.GetAsTree;

public sealed record GetOrganizationalUnitsAsTreeQuery : IQuery<List<OrganizationalUnitTreeResponse>>;
