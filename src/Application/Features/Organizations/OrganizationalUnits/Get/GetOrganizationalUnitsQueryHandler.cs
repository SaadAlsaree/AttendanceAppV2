using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Models;
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
        UserInfoDto user = await userContext.GetUserAsync();

        IQueryable<OrganizationalUnitResponse> organizationalUnitsQuery = context.OrganizationalUnits
            .Include(ou => ou.ParentUnit)
            .Include(ou => ou.Manager)
            .AsNoTracking()
            .AsSplitQuery()
            .Select(ou => new OrganizationalUnitResponse
            {
                Id = ou.Id,
                UnitName = ou.UnitName,
                UnitCode = ou.UnitCode,
                UnitDescription = ou.UnitDescription,
                ParentUnitId = ou.ParentUnitId,
                ParentUnitName = ou.ParentUnit != null ? ou.ParentUnit.UnitName : null,
                Email = ou.Email,
                PhoneNumber = ou.PhoneNumber,
                Address = ou.Address,
                PostalCode = ou.PostalCode,
                UnitLogo = ou.UnitLogo,
                UnitLevel = ou.UnitLevel,
                ManagerId = ou.ManagerId,
                ManagerName = ou.Manager != null ? ou.Manager.FullName : null,
                EmployeeCount = ou.Employees.Count,
                ChildUnitCount = ou.ChildUnits.Count,
                CreatedAt = ou.CreatedAt,
                UpdatedAt = ou.LastUpdatedAt
            });

        // Apply accessible unit filter if user is not Admin
        if (user.Role != Role.Admin)
        {
            IEnumerable<Guid> accessibleUnitIds = await hasPermission.GetAccessibleUnitIdsAsync(cancellationToken);
            organizationalUnitsQuery = organizationalUnitsQuery.Where(ou => accessibleUnitIds.Contains(ou.Id));
        }

        List<OrganizationalUnitResponse> organizationalUnits = await organizationalUnitsQuery.ToListAsync(cancellationToken);

        return organizationalUnits;
    }
}
