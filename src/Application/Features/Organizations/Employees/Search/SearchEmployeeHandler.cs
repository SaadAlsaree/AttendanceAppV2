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
    private const int MaxPageSize = 100;

    public async Task<Result<PaginatedResponse<EmployeeResponse>>> Handle(SearchEmployeeQuery query, CancellationToken cancellationToken)
    {
        int page = Math.Max(query.Page, 1);
        int pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        IQueryable<Employee> employeesQuery = context.Employees
            .Include(e => e.OrganizationalUnit)
            .Include(e => e.Manager)
            .Include(e => e.User)
            .AsNoTracking();

        // Same scoping rule as GET /employees (GetEmployeesQueryHandler): everyone except Admin only sees
        // employees inside their own organizational unit subtree.
        UserInfoDto currentUser = await userContext.GetUserAsync();
        bool isAdmin = currentUser.Role == Role.Admin;

        if (!isAdmin)
        {
            IEnumerable<Guid> accessibleUnitIds = await hasPermission.GetAccessibleUnitIdsAsync(cancellationToken);

            if (accessibleUnitIds == null || !accessibleUnitIds.Any())
            {
                return PaginatedResponse<EmployeeResponse>.Empty(page, pageSize);
            }

            employeesQuery = employeesQuery.Where(e => e.OrganizationalUnitId.HasValue && accessibleUnitIds.Contains(e.OrganizationalUnitId.Value));
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            string searchTerm = query.SearchTerm;
            employeesQuery = employeesQuery.Where(e =>
               EF.Functions.Like(e.FullName, $"%{searchTerm}%") ||
                EF.Functions.Like(e.Code, $"%{searchTerm}%") ||
                EF.Functions.Like(e.Email, $"%{searchTerm}%"));
        }

        int totalCount = await employeesQuery.CountAsync(cancellationToken);

        if (totalCount == 0)
        {
            return PaginatedResponse<EmployeeResponse>.Empty(page, pageSize);
        }

        // Sensitive identifiers (email, RFID, national-ID scans) are only returned to Admin.
        IReadOnlyList<EmployeeResponse> paginatedEmployees = await employeesQuery
            .OrderBy(e => e.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EmployeeResponse
            {
                Id = e.Id,
                EmployeeId = e.EmpID,
                Code = e.Code ?? string.Empty,
                RFID = isAdmin ? (e.RFID ?? string.Empty) : string.Empty,
                UserId = e.UserId ?? Guid.Empty,
                FullName = e.FullName,
                FirstName = e.FirstName,
                SecondName = e.SecondName,
                ThirdName = e.ThirdName,
                FourthName = e.FourthName ?? string.Empty,
                FamilyName = e.FamilyName ?? string.Empty,
                Email = isAdmin ? (e.Email ?? string.Empty) : string.Empty,
                OrganizationalUnitId = e.OrganizationalUnitId ?? Guid.Empty,
                OrganizationalUnitName = e.OrganizationalUnit != null ? e.OrganizationalUnit.UnitName : string.Empty,
                ManagerId = e.ManagerId,
                ManagerName = e.Manager != null ? e.Manager.FullName : string.Empty,
                IsManager = e.IsManager ?? false,
                CreatedAt = e.CreatedAt,
                FaceImageUrl = e.FaceImageUrl,
                NationalIdFrontUrl = isAdmin ? e.NationalIdFrontUrl : null,
                NationalIdBackUrl = isAdmin ? e.NationalIdBackUrl : null,
                ProfileImageUrl = e.ProfileImageUrl,
                Status = e.User != null ? Enum.Parse<UserStatus>(e.User.Status.ToString()) : UserStatus.Active,
                StatusName = e.User != null ? Enum.Parse<UserStatus>(e.User.Status.ToString()).ToString() : UserStatus.Active.ToString(),
            })
            .ToListAsync(cancellationToken);

        return PaginatedResponse<EmployeeResponse>.Create(
            paginatedEmployees,
            totalCount,
            page,
            pageSize);
    }
}
