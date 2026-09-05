using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Models;
using Domain.Entities.Organizations;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Organizations.Employees.Search;

internal sealed class SearchEmployeeHandler(
    IApplicationDbContext context,
    IHasPermission hasPermission,
    IUserContext userContext)
    : IQueryHandler<SearchEmployeeQuery, PaginatedResponse<EmployeeResponse>>
{
    public async Task<Result<PaginatedResponse<EmployeeResponse>>> Handle(SearchEmployeeQuery query, CancellationToken cancellationToken)
    {
        IQueryable<Employee> employeesQuery = context.Employees
            .Include(e => e.OrganizationalUnit)
            .Include(e => e.Manager)
            .Include(e => e.User)
            .AsNoTracking();

        // This search had no scoping at all, so any role reaching the endpoint could read the whole
        // employee directory. Mirror GetEmployeesQueryHandler.
        UserInfoDto currentUser = await userContext.GetUserAsync();
        if (currentUser.Role != Role.Admin)
        {
            IEnumerable<Guid> accessibleUnitIds = await hasPermission.GetAccessibleUnitIdsAsync(cancellationToken);
            var accessibleUnitIdList = accessibleUnitIds.ToList();

            if (accessibleUnitIdList.Count == 0)
            {
                return PaginatedResponse<EmployeeResponse>.Empty(query.Page, query.PageSize);
            }

            employeesQuery = employeesQuery.Where(e =>
                e.OrganizationalUnitId.HasValue && accessibleUnitIdList.Contains(e.OrganizationalUnitId.Value));
        }

        // Apply search term filter (required for search)
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            string searchTerm = query.SearchTerm;
            employeesQuery = employeesQuery.Where(e =>
               EF.Functions.Like(e.FullName, $"%{searchTerm}%") ||
                EF.Functions.Like(e.Code, $"%{searchTerm}%") ||
                EF.Functions.Like(e.Email, $"%{searchTerm}%"));
        }

        // Apply additional filters

        // Get total count for pagination
        int totalCount = await employeesQuery.CountAsync(cancellationToken);

        if (totalCount == 0)
        {
            return PaginatedResponse<EmployeeResponse>.Empty(query.Page, query.PageSize);
        }

        // Apply pagination
        IReadOnlyList<EmployeeResponse> paginatedEmployees = await employeesQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(e => new EmployeeResponse
            {
                Id = e.Id,
                EmployeeId = e.EmpID,
                Code = e.Code ?? string.Empty,
                RFID = e.RFID ?? string.Empty,
                UserId = e.UserId ?? Guid.Empty,
                FullName = e.FullName,
                FirstName = e.FirstName,
                SecondName = e.SecondName,
                ThirdName = e.ThirdName,
                FourthName = e.FourthName ?? string.Empty,
                FamilyName = e.FamilyName ?? string.Empty,
                Email = e.Email ?? string.Empty,
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
