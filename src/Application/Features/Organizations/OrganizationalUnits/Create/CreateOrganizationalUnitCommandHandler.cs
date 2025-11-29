using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Organizations;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Organizations.OrganizationalUnits.Create;

internal sealed class CreateOrganizationalUnitCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<CreateOrganizationalUnitCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateOrganizationalUnitCommand command, CancellationToken cancellationToken)
    {
        // Check if unit code is unique
        bool unitCodeExists = await context.OrganizationalUnits
            .AsNoTracking()
            .AnyAsync(u => u.UnitCode == command.UnitCode, cancellationToken);

        if (unitCodeExists)
        {
            return Result.Failure<Guid>(OrganizationErrors.OrganizationalUnit.UnitCodeAlreadyExists(command.UnitCode));
        }

        // Check if parent unit exists (if specified)
        if (command.ParentUnitId.HasValue)
        {
            bool parentUnitExists = await context.OrganizationalUnits
                .AsNoTracking()
                .AnyAsync(u => u.Id == command.ParentUnitId.Value, cancellationToken);

            if (!parentUnitExists)
            {
                return Result.Failure<Guid>(OrganizationErrors.OrganizationalUnit.ParentUnitNotFound(command.ParentUnitId.Value));
            }
        }

        // Check if manager exists (if specified)
        if (command.ManagerId.HasValue)
        {
            bool managerExists = await context.Employees
                .AsNoTracking()
                .AnyAsync(e => e.Id == command.ManagerId.Value, cancellationToken);

            if (!managerExists)
            {
                return Result.Failure<Guid>(EmployeeErrors.NotFound(command.ManagerId.Value));
            }
        }

        var organizationalUnit = new OrganizationalUnit
        {
            UnitName = command.UnitName,
            UnitCode = command.UnitCode,
            UnitDescription = command.UnitDescription,
            ParentUnitId = command.ParentUnitId,
            Email = command.Email,
            PhoneNumber = command.PhoneNumber,
            Address = command.Address,
            PostalCode = command.PostalCode,
            UnitLogo = command.UnitLogo,
            UnitLevel = command.UnitLevel,
            ManagerId = command.ManagerId,
            CreatedAt = dateTimeProvider.GetUtcNow()
        };

        context.OrganizationalUnits.Add(organizationalUnit);

        await context.SaveChangesAsync(cancellationToken);

        return organizationalUnit.Id;
    }
}
