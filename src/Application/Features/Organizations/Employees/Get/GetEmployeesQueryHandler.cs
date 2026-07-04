using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Models;
using Domain.Entities.Organizations;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Organizations.Employees.Get;

internal sealed class GetEmployeesQueryHandler(
    IApplicationDbContext context,
    IHasPermission hasPermission,
    IUserContext userContext)
    : IQueryHandler<GetEmployeesQuery, PaginatedResponse<EmployeeResponse>>
{
    public async Task<Result<PaginatedResponse<EmployeeResponse>>> Handle(GetEmployeesQuery query, CancellationToken cancellationToken)
    {
        IQueryable<Employee> employeesQuery = context.Employees
            .Include(e => e.OrganizationalUnit)
            .Include(e => e.Manager)
            .Include(e => e.User)
            .AsNoTracking();

        // Apply permission filter: if current user is not Admin, restrict to accessible unit ids (their unit + descendants)
        UserInfoDto currentUser = await userContext.GetUserAsync();
        if (currentUser.Role != Role.Admin)
        {
            IEnumerable<Guid> accessibleUnitIds = await hasPermission.GetAccessibleUnitIdsAsync(cancellationToken);

            // If user has no accessible units, return empty paginated response immediately
            if (accessibleUnitIds == null || !accessibleUnitIds.Any())
            {
                return PaginatedResponse<EmployeeResponse>.Empty(query.Page, query.PageSize);
            }

            employeesQuery = employeesQuery.Where(e => e.OrganizationalUnitId.HasValue && accessibleUnitIds.Contains(e.OrganizationalUnitId.Value));
        }

        // Apply filters
        if (query.OrganizationalUnitId.HasValue)
        {
            employeesQuery = employeesQuery.Where(e => e.OrganizationalUnitId == query.OrganizationalUnitId);
        }

        if (query.IsManager.HasValue)
        {
            employeesQuery = employeesQuery.Where(e => e.IsManager == query.IsManager);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            string searchTerm = query.SearchTerm;
            employeesQuery = employeesQuery.Where(e =>
               EF.Functions.Like(e.FullName, $"%{searchTerm}%") ||
                EF.Functions.Like(e.Code, $"%{searchTerm}%") ||
                EF.Functions.Like(e.EmpID, $"%{searchTerm}%"));
        }

        // Get total count for pagination
        int totalCount = await employeesQuery.CountAsync(cancellationToken);

        if (totalCount == 0)
        {
            return PaginatedResponse<EmployeeResponse>.Empty(query.Page, query.PageSize);
        }

        // Apply deterministic ordering (newest first) so pagination is stable and recently
        // added employees surface on the first page — required for assigning schedules to new hires.
        employeesQuery = employeesQuery
            .OrderByDescending(e => e.CreatedAt)
            .ThenBy(e => e.FullName);

        // Apply pagination
        IReadOnlyList<EmployeeResponse> paginatedEmployees = await employeesQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(e => new EmployeeResponse
            {
                Id = e.Id,
                RFID = e.RFID ?? string.Empty,
                UserId = e.UserId ?? Guid.Empty,
                EmpId = e.EmpID,
                FullName = e.FullName,
                OrganizationalUnitId = e.OrganizationalUnitId ?? Guid.Empty,
                OrganizationalUnitName = e.OrganizationalUnit != null ? e.OrganizationalUnit.UnitName : string.Empty,
                ManagerId = e.ManagerId,
                ManagerName = e.Manager != null ? e.Manager.FullName : string.Empty,
                IsManager = e.IsManager ?? false,
                CreatedAt = e.CreatedAt,
                FaceImageUrl = e.FaceImageUrl,
                NationalIdFrontUrl = e.NationalIdFrontUrl,
                NationalIdBackUrl = e.NationalIdBackUrl,
                ProfileImageUrl = e.ProfileImageUrl,
                Status = e.User != null ? Enum.Parse<UserStatus>(e.User.Status.ToString()) : UserStatus.Active,
                StatusName = e.User != null ? Enum.Parse<UserStatus>(e.User.Status.ToString()).ToString() : UserStatus.Active.ToString(),
            })
            .ToListAsync(cancellationToken);

        // Create paginated response
        return PaginatedResponse<EmployeeResponse>.Create(
            paginatedEmployees,
            totalCount,
            query.Page,
            query.PageSize);
    }
}
