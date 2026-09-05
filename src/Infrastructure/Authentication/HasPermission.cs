using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Models;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Authentication;

internal sealed class HasPermission : IHasPermission
{
    private readonly ILogger<HasPermission> _logger;
    private readonly IUserContext _currentUserService;
    private readonly IApplicationDbContext _context;

    public HasPermission(ILogger<HasPermission> logger, IUserContext currentUserService, IApplicationDbContext context)
    {
        _logger = logger;
        _currentUserService = currentUserService;
        _context = context;
    }

    public async Task<IEnumerable<Guid>> GetAccessibleUnitIdsAsync(CancellationToken cancellationToken = default)
    {
        UserInfoDto user = await _currentUserService.GetUserAsync();
        if (user.Id == Guid.Empty)
        {
            return Enumerable.Empty<Guid>();
        }

        try
        {
            Guid userId = user.Id;
            HashSet<Guid> accessibleUnitIds = new();

            // A SiteSupervisor is scoped by their site's EXPLICIT unit list. Membership is
            // non-transitive, so this path must never descend the tree — a unit being in the site
            // says nothing about its children. It also deliberately ignores the user's own
            // OrganizationalUnitId: a row may still carry one from a previous role, and unioning it
            // would silently widen the scope past the site.
            if (user.Role == Role.SiteSupervisor)
            {
                if (user.SiteId is not { } siteId)
                {
                    // No site assigned -> no scope. Every consumer treats an empty set as "nothing
                    // is accessible", so the role degrades closed rather than open.
                    return accessibleUnitIds;
                }

                List<Guid> siteUnitIds = await _context.OrganizationalUnits
                    .AsNoTracking()
                    .Where(ou => ou.SiteId == siteId && !ou.IsDeleted)
                    .Select(ou => ou.Id)
                    .ToListAsync(cancellationToken);

                accessibleUnitIds.UnionWith(siteUnitIds);
                return accessibleUnitIds;
            }

            // Get user's organizational unit
            var userUnit = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.OrganizationalUnitId })
                .AsSplitQuery()
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);

            if (userUnit?.OrganizationalUnitId is not { } userUnitId)
            {
                // _logger.LogDebug("User {UserId} does not belong to any organizational unit", userId);
                return accessibleUnitIds;
            }

            accessibleUnitIds.Add(userUnitId);

            // Get all sub-units recursively
            IEnumerable<Guid> subUnitIds = await GetAllSubUnitIdsAsync(userUnitId, cancellationToken);
            accessibleUnitIds.UnionWith(subUnitIds);

            // _logger.LogDebug("User {UserId} can access {UnitCount} organizational units", userId, accessibleUnitIds.Count);
            return accessibleUnitIds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving accessible unit IDs for user {UserId}", user.Id);
            return Enumerable.Empty<Guid>();
        }
    }

    public async Task<bool> CanManageEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        UserInfoDto user = await _currentUserService.GetUserAsync();

        // Global administrators may manage any employee — no unit restriction.
        if (user.Role is Role.Admin or Role.SuperAdmin)
        {
            return true;
        }

        // SiteSupervisor is strictly view-only. Without this deny it would fall through to the
        // accessible-unit check below and be authorized for every write inside its own site. This
        // is the one structural backstop against the role being added by mistake to a write
        // endpoint's allowedRoles — there are 91 of those files, each with its own copy-pasted list.
        if (user.Role == Role.SiteSupervisor)
        {
            return false;
        }

        // Everyone else is bound to their accessible unit tree. An employee with no
        // organizational unit is unreachable to a scoped role.
        Guid? employeeUnitId = await _context.Employees
            .Where(e => e.Id == employeeId)
            .Select(e => e.OrganizationalUnitId)
            .FirstOrDefaultAsync(cancellationToken);

        if (employeeUnitId is not { } unitId)
        {
            return false;
        }

        IEnumerable<Guid> accessibleUnitIds = await GetAccessibleUnitIdsAsync(cancellationToken);
        return accessibleUnitIds.Contains(unitId);
    }

    private async Task<IEnumerable<Guid>> GetAllSubUnitIdsAsync(Guid parentUnitId, CancellationToken cancellationToken)
    {
        var subUnitIds = new HashSet<Guid>();
        await GetAllSubUnitIdsRecursiveAsync(parentUnitId, subUnitIds, cancellationToken);
        return subUnitIds;
    }

    private async Task GetAllSubUnitIdsRecursiveAsync(Guid parentUnitId, HashSet<Guid> subUnitIds, CancellationToken cancellationToken)
    {
        IEnumerable<Guid> childUnits = await _context.OrganizationalUnits
            .Where(ou => ou.ParentUnitId == parentUnitId)
            .Select(ou => ou.Id)
            .ToListAsync(cancellationToken);

        foreach (Guid childUnitId in childUnits)
        {
            subUnitIds.Add(childUnitId);
            await GetAllSubUnitIdsRecursiveAsync(childUnitId, subUnitIds, cancellationToken);
        }
    }
}
