using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Organizations;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Organizations.Employees.AssignManager;

internal sealed class AssignManagerCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext)
    : ICommandHandler<AssignManagerCommand>
{
    public async Task<Result> Handle(AssignManagerCommand command, CancellationToken cancellationToken)
    {
        // Check if employee exists
        Employee employee = await context.Employees
            .FirstOrDefaultAsync(e => e.Id == command.Id, cancellationToken);

        if (employee is null)
        {
            return Result.Failure(EmployeeErrors.NotFound(command.Id));
        }

        // Check if manager exists
        Employee manager = await context.Employees
            .FirstOrDefaultAsync(e => e.Id == command.ManagerId, cancellationToken);

        if (manager is null)
        {
            return Result.Failure(EmployeeErrors.NotFound(command.ManagerId));
        }

        // Check if employee is not being assigned as their own manager
        if (command.Id == command.ManagerId)
        {
            return Result.Failure(EmployeeErrors.InvalidManagerAssignment(command.Id, manager.Id));
        }

        // Check if manager is active
        // Check if manager's user exists and is active
        if (manager.User is null || manager.User.Status != Domain.Enums.UserStatus.Active)
        {
            return Result.Failure(EmployeeErrors.InactiveEmployee(command.ManagerId));
        }


        // Assign manager
        employee.ManagerId = command.ManagerId;
        employee.LastUpdatedAt = dateTimeProvider.GetUtcNow();
        employee.LastUpdatedBy = userContext.UserId;

        // Raise domain event
        employee.Raise(new EmployeeManagerAssignedDomainEvent(employee.Id, manager.Id));

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
