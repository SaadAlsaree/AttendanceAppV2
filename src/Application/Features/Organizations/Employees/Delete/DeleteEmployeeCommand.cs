using Application.Abstractions.Messaging;

namespace Application.Features.Organizations.Employees.Delete;

public sealed class DeleteEmployeeCommand : ICommand
{
    public Guid Id { get; set; }
}
