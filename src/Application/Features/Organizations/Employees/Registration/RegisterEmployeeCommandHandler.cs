using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Organizations;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Organizations.Employees.Registration;

internal sealed class RegisterEmployeeCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<RegisterEmployeeCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RegisterEmployeeCommand command, CancellationToken cancellationToken)
    {
        // Check if employee number is unique
        bool employeeNumberExists = await context.Employees
            .AnyAsync(e => e.EmpID == command.EmpId, cancellationToken);

        if (employeeNumberExists)
        {
            return Result.Failure<Guid>(EmployeeErrors.DuplicateEmployeeNumber(command.EmpId));
        }

        // Check if organizational unit exists
        OrganizationalUnit? organizationalUnit = await context.OrganizationalUnits
            .SingleOrDefaultAsync(o => o.Id == command.OrganizationalUnitId, cancellationToken);

        if (organizationalUnit is null)
        {
            return Result.Failure<Guid>(OrganizationErrors.OrganizationalUnit.NotFound(command.OrganizationalUnitId));
        }

        // Check if manager exists if provided
        if (command.ManagerId.HasValue)
        {
            Employee? manager = await context.Employees
                .SingleOrDefaultAsync(e => e.Id == command.ManagerId.Value, cancellationToken);

            if (manager is null)
            {
                return Result.Failure<Guid>(EmployeeErrors.NotFound(command.ManagerId.Value));
            }
        }

        // Generate full name
        string fullName = $"{command.FirstName} {command.SecondName} {command.ThirdName} {command.FourthName} {command.FamilyName}".Trim();





        // Create employee
        var employee = new Employee
        {
            EmpID = command.EmpId,
            RFID = command.RFID,
            FirstName = command.FirstName,
            SecondName = command.SecondName,
            ThirdName = command.ThirdName,
            FourthName = command.FourthName,
            FamilyName = command.FamilyName,
            FullName = fullName,
            OrganizationalUnitId = command.OrganizationalUnitId,
            ManagerId = command.ManagerId,
            IsManager = command.IsManager,
            CreatedAt = dateTimeProvider.GetUtcNow(),
        };


        employee.Raise(new EmployeeCreatedDomainEvent(employee.Id, employee.EmpID));

        // Add to context

        context.Employees.Add(employee);

        await context.SaveChangesAsync(cancellationToken);

        return employee.Id;
    }
}
