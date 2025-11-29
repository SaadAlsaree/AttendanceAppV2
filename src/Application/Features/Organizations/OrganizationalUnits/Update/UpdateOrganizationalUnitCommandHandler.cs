using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Organizations;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Organizations.OrganizationalUnits.Update;

internal sealed class UpdateOrganizationalUnitCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<UpdateOrganizationalUnitCommand>
{
    public async Task<Result> Handle(UpdateOrganizationalUnitCommand command, CancellationToken cancellationToken)
    {
        OrganizationalUnit? organizationalUnit = await context.OrganizationalUnits
            .SingleOrDefaultAsync(ou => ou.Id == command.OrganizationalUnitId, cancellationToken);

        if (organizationalUnit is null)
        {
            return Result.Failure(OrganizationErrors.OrganizationalUnit.NotFound(command.OrganizationalUnitId));
        }


        //// Check if parent unit exists (if specified)
        //if (command.ParentUnitId.HasValue)
        //{
        //    bool parentUnitExists = await context.OrganizationalUnits
        //        .AsNoTracking()
        //        .AnyAsync(u => u.Id == command.ParentUnitId.Value, cancellationToken);

        //    if (!parentUnitExists)
        //    {
        //        return Result.Failure(OrganizationErrors.OrganizationalUnit.ParentUnitNotFound(command.ParentUnitId.Value));
        //    }

        //    // Prevent circular reference
        //    if (command.ParentUnitId.Value == command.OrganizationalUnitId)
        //    {
        //        return Result.Failure(OrganizationErrors.OrganizationalUnit.ParentUnitNotFound(command.ParentUnitId.Value));
        //    }
        //}



        organizationalUnit.UnitName = command.UnitName;
        organizationalUnit.UnitCode = command.UnitCode;
        organizationalUnit.UnitDescription = command.UnitDescription;
        organizationalUnit.ParentUnitId = command.ParentUnitId;
        organizationalUnit.Email = command.Email;
        organizationalUnit.PhoneNumber = command.PhoneNumber;
        organizationalUnit.Address = command.Address;
        organizationalUnit.PostalCode = command.PostalCode;
        organizationalUnit.UnitLogo = command.UnitLogo;
        organizationalUnit.UnitLevel = command.UnitLevel;
        organizationalUnit.ManagerId = command.ManagerId;
        organizationalUnit.LastUpdatedAt = dateTimeProvider.GetUtcNow();

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
