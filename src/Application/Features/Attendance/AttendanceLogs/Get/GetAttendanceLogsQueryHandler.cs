using System.Globalization;
using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Models;
using Domain.Entities.Attendance;
using Domain.Entities.Organizations;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Attendance.AttendanceLogs.Get;

internal sealed class GetAttendanceLogsQueryHandler(
    IApplicationDbContext context,
    IHasPermission hasPermission,
    IUserContext userContext)
    : IQueryHandler<GetAttendanceLogsQuery, PaginatedResponse<AttendanceLogResponse>>
{
    public async Task<Result<PaginatedResponse<AttendanceLogResponse>>> Handle(GetAttendanceLogsQuery query, CancellationToken cancellationToken)
    {
        // Build base query
        IQueryable<AttendanceLog> queryable = context.AttendanceLogs
            .AsNoTracking()
            .AsQueryable();

        // Apply permission filter: if current user is not Admin, restrict to accessible unit ids
        UserInfoDto currentUser = await userContext.GetUserAsync();
        if (currentUser.Role != Role.Admin)
        {
            IEnumerable<Guid> accessibleUnitIds = await hasPermission.GetAccessibleUnitIdsAsync(cancellationToken);

            // If user has no accessible units, return empty paginated response immediately
            if (accessibleUnitIds is null || !accessibleUnitIds.Any())
            {
                return PaginatedResponse<AttendanceLogResponse>.Empty(query.Page, query.PageSize);
            }

            // Filter by employee's organizational unit
            queryable = from log in queryable
                        join emp in context.Employees on log.EmpID equals emp.EmpID
                        where emp.OrganizationalUnitId.HasValue &&
                              accessibleUnitIds.Contains(emp.OrganizationalUnitId.Value)
                        select log;
        }

        // Apply OrganizationId filter
        if (query.OrganizationId.HasValue)
        {
            queryable = from log in queryable
                        join emp in context.Employees on log.EmpID equals emp.EmpID
                        where emp.OrganizationalUnitId == query.OrganizationId.Value
                        select log;
        }

        // Apply StartDate filter
        if (query.StartDate.HasValue)
        {
            queryable = queryable.Where(al => al.DateTimeAttend >= query.StartDate.Value);
        }

        // Apply EndDate filter
        if (query.EndDate.HasValue)
        {
            queryable = queryable.Where(al => al.DateTimeAttend <= query.EndDate.Value);
        }

        // Apply Direct filter
        if (query.Direct.HasValue)
        {
            queryable = queryable.Where(al => al.Direct == query.Direct.Value);
        }

        // Apply DeviceName filter
        if (!string.IsNullOrWhiteSpace(query.DeviceName))
        {
            queryable = queryable.Where(al => al.DeviceName == query.DeviceName);
        }

        // Apply DeviceNo filter
        if (!string.IsNullOrWhiteSpace(query.DeviceNo))
        {
            queryable = queryable.Where(al => al.DeviceNo == query.DeviceNo);
        }

        // Apply EmpID filter
        if (!string.IsNullOrWhiteSpace(query.EmpID))
        {
            queryable = queryable.Where(al => al.EmpID == query.EmpID);
        }

        // Apply EmpName filter
        if (!string.IsNullOrWhiteSpace(query.EmpName))
        {
            queryable = from log in queryable
                        join emp in context.Employees on log.EmpID equals emp.EmpID
                        where EF.Functions.Like(emp.FullName, $"%{query.EmpName}%")
                        select log;
        }

        // Apply SearchTerm filter
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            string searchTerm = query.SearchTerm.Trim();
            queryable = from log in queryable
                        join emp in context.Employees on log.EmpID equals emp.EmpID
                        where EF.Functions.Like(emp.FullName, $"%{searchTerm}%") ||
                              EF.Functions.Like(emp.Code ?? string.Empty, $"%{searchTerm}%") ||
                              EF.Functions.Like(log.EmpID, $"%{searchTerm}%") ||
                              EF.Functions.Like(log.CardNo, $"%{searchTerm}%")
                        select log;
        }

        // Apply sorting
        string? sortOrder = query.SortOrder?.ToUpper(CultureInfo.InvariantCulture);
        bool isDescending = sortOrder == "DESC";

        queryable = query.SortBy?.ToUpper(CultureInfo.InvariantCulture) switch
        {
            "DATETIMEATTEND" or "TIME" => isDescending
                ? queryable.OrderByDescending(al => al.DateTimeAttend)
                : queryable.OrderBy(al => al.DateTimeAttend),
            "EMPLOYEENAME" or "EMPNAME" => isDescending
                ? queryable.Join(context.Employees,
                    log => log.EmpID,
                    emp => emp.EmpID,
                    (log, emp) => new { Log = log, Employee = emp })
                    .OrderByDescending(x => x.Employee.FullName)
                    .Select(x => x.Log)
                : queryable.Join(context.Employees,
                    log => log.EmpID,
                    emp => emp.EmpID,
                    (log, emp) => new { Log = log, Employee = emp })
                    .OrderBy(x => x.Employee.FullName)
                    .Select(x => x.Log),
            "CARDNO" => isDescending
                ? queryable.OrderByDescending(al => al.CardNo)
                : queryable.OrderBy(al => al.CardNo),
            "DIRECT" => isDescending
                ? queryable.OrderByDescending(al => al.Direct)
                : queryable.OrderBy(al => al.Direct),
            "DEVICENAME" => isDescending
                ? queryable.OrderByDescending(al => al.DeviceName)
                : queryable.OrderBy(al => al.DeviceName),
            _ => queryable.OrderByDescending(al => al.DateTimeAttend)
        };

        // Get total count
        int totalCount = await queryable.CountAsync(cancellationToken);

        if (totalCount == 0)
        {
            return PaginatedResponse<AttendanceLogResponse>.Empty(query.Page, query.PageSize);
        }

        // Apply pagination
        List<AttendanceLog> logs = await queryable
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        // Get employee and attendance data for the logs
        var empIds = logs.Select(l => l.EmpID).Distinct().ToList();
        Dictionary<string, Employee> employees = await context.Employees
            .Include(e => e.OrganizationalUnit)
            .Where(e => empIds.Contains(e.EmpID))
            .ToDictionaryAsync(e => e.EmpID, cancellationToken);

        // Map to response
        var response = logs.Select(log =>
        {
            Employee? employee = employees.GetValueOrDefault(log.EmpID);

            return new AttendanceLogResponse
            {
                Id = log.Id,
                DateTimeAttend = log.DateTimeAttend,
                CardNo = log.CardNo,
                EmpID = log.EmpID,
                DateWork = log.DateWork,
                TimeAttend = log.TimeAttend,
                Direct = log.Direct,
                DeviceName = log.DeviceName,
                DeviceNo = log.DeviceNo,
                EmpName = employee?.FullName ?? string.Empty,
                Employee = employee is not null ? CreateEmployeeResponse(employee) : null
            };
        }).ToList();

        return PaginatedResponse<AttendanceLogResponse>.Create(
            response,
            totalCount,
            query.Page,
            query.PageSize);
    }

    private static EmployeeResponse CreateEmployeeResponse(
        Employee employee)
    {
        return new EmployeeResponse
        {
            Id = employee.Id,
            FullName = employee.FullName,
            Code = employee.Code ?? string.Empty,
            RFID = employee.RFID ?? string.Empty,
            OrganizationalUnitId = employee.OrganizationalUnitId?.ToString() ?? string.Empty,
            OrganizationalUnitName = employee.OrganizationalUnit?.UnitName ?? string.Empty,
            OrganizationalUnitCode = employee.OrganizationalUnit?.UnitCode ?? string.Empty
        };
    }
}
