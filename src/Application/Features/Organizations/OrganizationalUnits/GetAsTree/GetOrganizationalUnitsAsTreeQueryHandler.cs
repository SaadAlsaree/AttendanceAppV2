using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Models;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Organizations.OrganizationalUnits.GetAsTree;

internal sealed class GetOrganizationalUnitsAsTreeQueryHandler(IApplicationDbContext context,
    IHasPermission hasPermission,
    IUserContext userContext)
    : IQueryHandler<GetOrganizationalUnitsAsTreeQuery, List<OrganizationalUnitTreeResponse>>
{
    public async Task<Result<List<OrganizationalUnitTreeResponse>>> Handle(GetOrganizationalUnitsAsTreeQuery query, CancellationToken cancellationToken)
    {
        // Get all organizational units with their relationships
        List<Domain.Entities.Organizations.OrganizationalUnit> allUnits = await context.OrganizationalUnits
            .Include(ou => ou.ParentUnit)
            .Include(ou => ou.Manager)
            .Include(ou => ou.ChildUnits)
            .Include(ou => ou.Employees)
            .AsNoTracking()
            .AsSplitQuery()
            .Where(ou => !ou.IsDeleted)
            .ToListAsync(cancellationToken);


        // check if user role not Admin then apply accessible unit ids filter
        UserInfoDto user = await userContext.GetUserAsync();
        if (user.Role != Role.Admin)
        {
            IEnumerable<Guid> accessibleUnitIds = await hasPermission.GetAccessibleUnitIdsAsync(cancellationToken);
            allUnits = allUnits.Where(ou => accessibleUnitIds.Contains(ou.Id)).ToList();
        }

        // Build flat response objects
        var unitResponses = allUnits.Select(ou => new OrganizationalUnitTreeResponse
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
            UpdatedAt = ou.LastUpdatedAt,
            Children = new List<OrganizationalUnitTreeResponse>()
        }).ToList();

        // Build tree structure
        List<OrganizationalUnitTreeResponse> tree = BuildTree(unitResponses);

        // Calculate aggregated counts
        CalculateAggregatedCounts(tree);

        return tree;
    }

    private static List<OrganizationalUnitTreeResponse> BuildTree(List<OrganizationalUnitTreeResponse> units)
    {
        var lookup = units.ToDictionary(u => u.Id);
        var rootUnits = new List<OrganizationalUnitTreeResponse>();

        foreach (OrganizationalUnitTreeResponse unit in units)
        {
            if (unit.ParentUnitId == null)
            {
                // This is a root unit
                rootUnits.Add(unit);
            }
            else if (lookup.TryGetValue(unit.ParentUnitId.Value, out OrganizationalUnitTreeResponse parent))
            {
                // This unit has a parent, add it as a child
                parent.Children.Add(unit);
            }
        }

        return rootUnits;
    }

    private static void CalculateAggregatedCounts(List<OrganizationalUnitTreeResponse> tree)
    {
        foreach (OrganizationalUnitTreeResponse unit in tree)
        {
            CalculateAggregatedCountsRecursive(unit);
        }
    }

    private static void CalculateAggregatedCountsRecursive(OrganizationalUnitTreeResponse unit)
    {
        unit.TotalEmployeeCount = unit.EmployeeCount;
        unit.TotalChildUnitCount = unit.ChildUnitCount;

        foreach (OrganizationalUnitTreeResponse child in unit.Children)
        {
            CalculateAggregatedCountsRecursive(child);
            unit.TotalEmployeeCount += child.TotalEmployeeCount;
            unit.TotalChildUnitCount += child.TotalChildUnitCount + 1; // +1 for the child itself
        }
    }
}
