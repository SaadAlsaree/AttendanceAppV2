using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Models;
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
