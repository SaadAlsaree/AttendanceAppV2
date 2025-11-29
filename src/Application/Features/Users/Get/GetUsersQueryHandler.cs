using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Models;
using Domain.Entities.Users;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Users.Get;

internal sealed class GetUsersQueryHandler(
    IApplicationDbContext context,
    IHasPermission hasPermission,
    IUserContext userContext)
    : IQueryHandler<GetUsersQuery, PaginatedResponse<UserResponse>>
{
    public async Task<Result<PaginatedResponse<UserResponse>>> Handle(GetUsersQuery query, CancellationToken cancellationToken)
    {
        IQueryable<User> usersQuery = context.Users
            .Include(u => u.OrganizationalUnit)
            .AsNoTracking()
            .AsSplitQuery();

        // check if user role not Admin then apply accessible unit ids filter

        UserInfoDto user = await userContext.GetUserAsync();

        if (user.Role != Role.Admin)
        {
            IEnumerable<Guid> accessibleUnitIds = await hasPermission.GetAccessibleUnitIdsAsync(cancellationToken);
            usersQuery = usersQuery.Where(u => accessibleUnitIds.Contains(u.OrganizationalUnitId!.Value));
        }

        // Apply filters
        if (query.Role.HasValue)
        {
            usersQuery = usersQuery.Where(u => u.Role == query.Role.Value);
        }

        if (query.Status.HasValue)
        {
            usersQuery = usersQuery.Where(u => u.Status == query.Status.Value);
        }

        if (query.IsActive.HasValue)
        {
            usersQuery = usersQuery.Where(u => u.IsActive == query.IsActive.Value);
        }

        // Apply search
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            string searchTerm = query.SearchTerm.ToUpperInvariant();
            usersQuery = usersQuery.Where(u =>
                u.UserLogin.ToUpperInvariant().Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
        }

        // Apply sorting
        if (!string.IsNullOrWhiteSpace(query.SortBy))
        {
            string sortBy = query.SortBy.ToUpperInvariant();
            usersQuery = sortBy switch
            {
                "USERLOGIN" => usersQuery.OrderBy(u => u.UserLogin),
                "ROLE" => usersQuery.OrderBy(u => u.Role),
                "STATUS" => usersQuery.OrderBy(u => u.Status),
                "CREATEDAT" => usersQuery.OrderBy(u => u.CreatedAt),
                "LASTLOGINDATE" => usersQuery.OrderBy(u => u.LastLoginDate),
                _ => usersQuery.OrderBy(u => u.UserLogin)
            };
        }
        else
        {
            usersQuery = usersQuery.OrderBy(u => u.UserLogin);
        }

        // Get total count
        int totalCount = await usersQuery.CountAsync(cancellationToken);

        // Apply pagination
        List<UserResponse> users = await usersQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(u => new UserResponse
            {
                Id = u.Id,
                Username = u.Username,
                UserLogin = u.UserLogin,
                Role = u.Role,
                Status = u.Status,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                LastLoginDate = u.LastLoginDate,
                OrganizationalUnitId = u.OrganizationalUnitId,
                OrganizationalUnitName = u.OrganizationalUnit != null ? u.OrganizationalUnit.UnitName : null,
                OrganizationalUnitCode = u.OrganizationalUnit != null ? u.OrganizationalUnit.UnitCode : null
            })
            .ToListAsync(cancellationToken);

        return PaginatedResponse<UserResponse>.Create(
            users,
            totalCount,
            query.Page,
            query.PageSize);
    }
}
