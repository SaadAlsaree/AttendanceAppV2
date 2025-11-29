using Application.Abstractions.Messaging;

namespace Application.Features.Organizations.Employees.AssignManager;

public sealed class AssignManagerCommand : ICommand
{
    public Guid Id { get; set; }
    public Guid ManagerId { get; set; }
}
