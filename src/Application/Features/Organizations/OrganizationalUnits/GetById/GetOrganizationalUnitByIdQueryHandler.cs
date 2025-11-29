using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Models;
using Domain.Entities.Organizations;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Organizations.OrganizationalUnits.GetById;

internal sealed class GetOrganizationalUnitByIdQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetOrganizationalUnitByIdQuery, OrganizationalUnitResponse>
{
    public async Task<Result<OrganizationalUnitResponse>> Handle(GetOrganizationalUnitByIdQuery query, CancellationToken cancellationToken)
    {
        OrganizationalUnitResponse? organizationalUnit = await context.OrganizationalUnits
            .Where(ou => ou.Id == query.OrganizationalUnitId)
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
            })
            .SingleOrDefaultAsync(cancellationToken);

        // check if user role not Admin then apply accessible unit ids filter


        if (organizationalUnit is null)
        {
            return Result.Failure<OrganizationalUnitResponse>(OrganizationErrors.OrganizationalUnit.NotFound(query.OrganizationalUnitId));
        }



        return organizationalUnit;
    }
}
