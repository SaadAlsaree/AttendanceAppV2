using Application.Abstractions.Messaging;

namespace Application.Organizations.OrganizationalUnits.Delete;

public sealed record DeleteOrganizationalUnitCommand(Guid OrganizationalUnitId) : ICommand;
