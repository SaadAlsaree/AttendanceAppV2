using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Organizations;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Organizations.Employees.Update;

internal sealed class UpdateEmployeeCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext)
    : ICommandHandler<UpdateEmployeeCommand>
{
    public async Task<Result> Handle(UpdateEmployeeCommand command, CancellationToken cancellationToken)
    {
        Employee employee = await context.Employees
            .FirstOrDefaultAsync(e => e.Id == command.Id, cancellationToken);

        if (employee is null)
        {
            return Result.Failure(EmployeeErrors.NotFound(command.Id));
        }



        // Check if organizational unit exists
        OrganizationalUnit? organizationalUnit = await context.OrganizationalUnits
            .SingleOrDefaultAsync(o => o.Id == command.OrganizationalUnitId, cancellationToken);

        if (organizationalUnit is null)
        {
            return Result.Failure(OrganizationErrors.OrganizationalUnit.NotFound(command.OrganizationalUnitId));
        }

        // Generate full name
        string fullName = $"{command.FirstName} {command.SecondName} {command.ThirdName} {command.FourthName} {command.FamilyName}".Trim();

        // Update employee
        employee.FirstName = command.FirstName;
        employee.SecondName = command.SecondName;
        employee.ThirdName = command.ThirdName;
        employee.FourthName = command.FourthName;
        employee.FamilyName = command.FamilyName;
        employee.FullName = fullName;
        employee.RFID = command.RFID;
        employee.EmpID = command.EmpId;
        employee.ManagerId = command.ManagerId;
        employee.OrganizationalUnitId = command.OrganizationalUnitId;
        employee.IsManager = command.IsManager;
        employee.LastUpdatedAt = dateTimeProvider.GetUtcNow();
        employee.LastUpdatedBy = userContext.UserId;

        // Raise domain event
        employee.Raise(new EmployeeUpdatedDomainEvent(employee.Id, employee.EmpID ?? string.Empty));

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
