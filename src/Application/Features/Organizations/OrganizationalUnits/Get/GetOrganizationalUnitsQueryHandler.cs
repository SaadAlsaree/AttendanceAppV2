using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Models;
using Domain.Entities.Organizations;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Organizations.OrganizationalUnits.Get;

internal sealed class GetOrganizationalUnitsQueryHandler(
    IApplicationDbContext context,
    IHasPermission hasPermission,
    IUserContext userContext)
    : IQueryHandler<GetOrganizationalUnitsQuery, List<OrganizationalUnitResponse>>
{
    public async Task<Result<List<OrganizationalUnitResponse>>> Handle(GetOrganizationalUnitsQuery query, CancellationToken cancellationToken)
    {
        // Get all active organizational units with their relationships
        List<OrganizationalUnit> allUnits = await context.OrganizationalUnits
            .Include(ou => ou.ParentUnit)
            .Include(ou => ou.Manager)
            .Include(ou => ou.ChildUnits)
            .Include(ou => ou.Employees)
            .AsNoTracking()
            .AsSplitQuery()
            .Where(ou => !ou.IsDeleted)
            .ToListAsync(cancellationToken);

        // Apply accessible unit filter if user is not Admin / SuperAdmin
        UserInfoDto user = await userContext.GetUserAsync();
        if (user.Role is not (Role.Admin or Role.SuperAdmin))
        {
            IEnumerable<Guid> accessibleUnitIds = await hasPermission.GetAccessibleUnitIdsAsync(cancellationToken);
            allUnits = allUnits.Where(ou => accessibleUnitIds.Contains(ou.Id)).ToList();
        }

        var organizationalUnits = allUnits.Select(ou => new OrganizationalUnitResponse
        {
            Id = ou.Id,
            UnitName = ou.UnitName,
            UnitCode = ou.UnitCode,
            UnitDescription = ou.UnitDescription,
            ParentUnitId = ou.ParentUnitId,
            ParentUnitName = ou.ParentUnit?.UnitName,
            Email = ou.Email,
            PhoneNumber = ou.PhoneNumber,
            Address = ou.Address,
            PostalCode = ou.PostalCode,
            UnitLogo = ou.UnitLogo,
            UnitLevel = ou.UnitLevel,
            ManagerId = ou.ManagerId,
            ManagerName = ou.Manager?.FullName,
            EmployeeCount = ou.Employees.Count,
            ChildUnitCount = ou.ChildUnits.Count,
            CreatedAt = ou.CreatedAt,
            UpdatedAt = ou.LastUpdatedAt
        }).ToList();

        return organizationalUnits;
    }
}
