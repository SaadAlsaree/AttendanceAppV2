using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Organizations;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Organizations.OrganizationalUnits.Delete;

internal sealed class DeleteOrganizationalUnitCommandHandler(IApplicationDbContext context)
    : ICommandHandler<DeleteOrganizationalUnitCommand>
{
    public async Task<Result> Handle(DeleteOrganizationalUnitCommand command, CancellationToken cancellationToken)
    {
        OrganizationalUnit? organizationalUnit = await context.OrganizationalUnits
            .Include(ou => ou.ChildUnits)
            .Include(ou => ou.Employees)
            .SingleOrDefaultAsync(ou => ou.Id == command.OrganizationalUnitId, cancellationToken);

        if (organizationalUnit is null)
        {
            return Result.Failure(OrganizationErrors.OrganizationalUnit.NotFound(command.OrganizationalUnitId));
        }

        // Check if unit has child units
        if (organizationalUnit.ChildUnits.Any())
        {
            return Result.Failure(OrganizationErrors.OrganizationalUnit.HasChildUnits);
        }

        // Check if unit has employees
        if (organizationalUnit.Employees.Any())
        {
            return Result.Failure(OrganizationErrors.OrganizationalUnit.HasEmployees);
        }

        context.OrganizationalUnits.Remove(organizationalUnit);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
