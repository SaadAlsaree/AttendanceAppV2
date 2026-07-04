using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Models;
using Domain.Entities.Organizations;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Organizations.EmployeeWeeklyShifts.Get;

internal sealed class GetEmployeeWeeklyShiftsQueryHandler(
    IApplicationDbContext context,
    IHasPermission hasPermission,
    IUserContext userContext)
    : IQueryHandler<GetEmployeeWeeklyShiftsQuery, PaginatedResponse<EmployeeWeeklyShiftsResponse>>
{
    public async Task<Result<PaginatedResponse<EmployeeWeeklyShiftsResponse>>> Handle(
        GetEmployeeWeeklyShiftsQuery query,
        CancellationToken cancellationToken)
    {
        // Page over EMPLOYEES that have a pattern (not over the pattern rows),
        // so pagination and counts are per employee.
        IQueryable<Employee> employeesQuery = context.Employees
            .AsNoTracking()
            .Where(e => !e.IsDeleted && e.WeeklyShifts.Any());

        // check if user role not Admin then apply accessible unit ids filter
        UserInfoDto user = await userContext.GetUserAsync();
        if (user.Role != Role.Admin)
        {
            IEnumerable<Guid> accessibleUnitIds = await hasPermission.GetAccessibleUnitIdsAsync(cancellationToken);
            employeesQuery = employeesQuery.Where(e =>
                e.OrganizationalUnitId.HasValue && accessibleUnitIds.Contains(e.OrganizationalUnitId.Value));
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            string searchTerm = query.SearchTerm.Trim();
            employeesQuery = employeesQuery.Where(e =>
                e.FullName.Contains(searchTerm) ||
                e.EmpID.Contains(searchTerm));
        }

        int totalCount = await employeesQuery.CountAsync(cancellationToken);

        int page = query.Page ?? 1;
        int pageSize = query.PageSize ?? 10;

        List<EmployeeWeeklyShiftsResponse> rows = await employeesQuery
            .OrderBy(e => e.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EmployeeWeeklyShiftsResponse
            {
                EmployeeId = e.Id,
                FullName = e.FullName,
                EmpId = e.EmpID,
                OrganizationalUnitName = e.OrganizationalUnit != null ? e.OrganizationalUnit.UnitName : null,
                Days = e.WeeklyShifts
                    .OrderBy(w => w.DayOfWeek)
                    .Select(w => new WeeklyShiftDayResponse
                    {
                        DayOfWeek = (int)w.DayOfWeek,
                        ShiftId = w.ShiftId,
                        ShiftName = w.Shift.Name,
                        StartTime = w.Shift.StartTime,
                        EndTime = w.Shift.EndTime
                    })
                    .ToList()
            })
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        var paginatedResponse = new PaginatedResponse<EmployeeWeeklyShiftsResponse>
        {
            Data = rows,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };

        return paginatedResponse;
    }
}
