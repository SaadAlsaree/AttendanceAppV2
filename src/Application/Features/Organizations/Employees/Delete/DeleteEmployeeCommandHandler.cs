using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Organizations;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Organizations.Employees.Delete;

internal sealed class DeleteEmployeeCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext)
    : ICommandHandler<DeleteEmployeeCommand>
{
    public async Task<Result> Handle(DeleteEmployeeCommand command, CancellationToken cancellationToken)
    {
        Employee employee = await context.Employees
            .Include(e => e.Subordinates)
            .FirstOrDefaultAsync(e => e.Id == command.Id, cancellationToken);

        if (employee is null)
        {
            return Result.Failure(EmployeeErrors.NotFound(command.Id));
        }

        // Check if employee has subordinates
        if (employee.Subordinates.Any())
        {
            return Result.Failure(new Error(
                "Employee.HasSubordinates",
                "Cannot delete an employee who has subordinates. Please reassign subordinates first.",
                ErrorType.Conflict));
        }

        // Soft delete the employee
        employee.IsDeleted = true;
        employee.DeletedAt = dateTimeProvider.GetUtcNow();
        employee.DeletedBy = userContext.UserId;

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
