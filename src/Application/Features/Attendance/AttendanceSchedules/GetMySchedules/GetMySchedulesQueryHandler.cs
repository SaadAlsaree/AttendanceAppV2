using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Features.Attendance.AttendanceSchedules.shared;
using Application.Models;
using Domain.Entities.Organizations;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Attendance.AttendanceSchedules.GetMySchedules;

internal sealed class GetMySchedulesQueryHandler(
    IApplicationDbContext context,
    IHasPermission hasPermission,
    IUserContext userContext)
    : IQueryHandler<GetMySchedulesQuery, PaginatedResponse<AttendanceScheduleResponse>>
{
    public async Task<Result<PaginatedResponse<AttendanceScheduleResponse>>> Handle(GetMySchedulesQuery query, CancellationToken cancellationToken)
    {
        // Get current user's employee ID
        Guid currentUserId = userContext.UserId;

        Employee? currentEmployee = await context.Employees
            .AsNoTracking()
            .SingleOrDefaultAsync(e => e.UserId == currentUserId, cancellationToken);

        if (currentEmployee is null)
        {
            return Result.Failure<PaginatedResponse<AttendanceScheduleResponse>>(EmployeeErrors.NotFound(currentEmployee!.Id));
        }

        IQueryable<Domain.Entities.Attendance.AttendanceSchedule> queryable = context.AttendanceSchedules
              .Include(s => s.Employee)
                .ThenInclude(e => e.OrganizationalUnit)
            .Include(s => s.ScheduleDays)
                .ThenInclude(sd => sd.Shift)
            .AsNoTracking()
            .Where(s => s.EmployeeId == currentEmployee.Id);

        // check if user role not Admin then apply accessible unit ids filter
        UserInfoDto user = await userContext.GetUserAsync();
        if (user.Role != Role.Admin)
        {
            IEnumerable<Guid> accessibleUnitIds = await hasPermission.GetAccessibleUnitIdsAsync(cancellationToken);
            queryable = queryable.Where(s => accessibleUnitIds.Contains(s.Employee.OrganizationalUnitId!.Value));
        }

        // Apply filters
        if (query.ScheduleType.HasValue)
        {
            queryable = queryable.Where(s => s.ScheduleType == query.ScheduleType.Value);
        }

        if (query.IsActive.HasValue)
        {
            queryable = queryable.Where(s => s.IsActive == query.IsActive.Value);
        }

        // Apply sorting
        if (!string.IsNullOrWhiteSpace(query.SortBy))
        {
            string? sortOrder = query.SortOrder?.ToUpperInvariant();
            bool isDescending = sortOrder == "DESC";

            queryable = query.SortBy.ToUpperInvariant() switch
            {
                "STARTDATE" => isDescending
                    ? queryable.OrderByDescending(s => s.StartDate)
                    : queryable.OrderBy(s => s.StartDate),
                "SCHEDULETYPE" => isDescending
                    ? queryable.OrderByDescending(s => s.ScheduleType)
                    : queryable.OrderBy(s => s.ScheduleType),
                "ISACTIVE" => isDescending
                    ? queryable.OrderByDescending(s => s.IsActive)
                    : queryable.OrderBy(s => s.IsActive),
                "CREATEDAT" => isDescending
                    ? queryable.OrderByDescending(s => s.CreatedAt)
                    : queryable.OrderBy(s => s.CreatedAt),
                _ => queryable.OrderByDescending(s => s.CreatedAt)
            };
        }
        else
        {
            queryable = queryable.OrderByDescending(s => s.CreatedAt);
        }

        int totalCount = await queryable.CountAsync(cancellationToken);

        List<AttendanceScheduleResponse> schedules = await queryable
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(s => new AttendanceScheduleResponse
            {
                Id = s.Id,
                EmployeeId = s.EmployeeId,
                EmployeeName = s.Employee.FullName,
                EmployeeEmail = s.Employee.Email,
                EmployeeOrganizationId = s.Employee.OrganizationalUnitId ?? Guid.Empty,
                EmployeeOrganizationName = s.Employee.OrganizationalUnit != null ? s.Employee.OrganizationalUnit.UnitName : string.Empty,
                StartDate = s.StartDate,
                EndDate = s.EndDate,

                ScheduleDays = s.ScheduleDays
                    .OrderBy(sd => sd.ScheduleDayDate)
                    .Select(sd => new ScheduleDayResponse
                    {
                        Id = sd.Id,
                        AttendanceScheduleId = sd.AttendanceScheduleId,
                        ScheduleDayDate = sd.ScheduleDayDate,
                        ShiftId = sd.ShiftId,
                        ShiftName = sd.Shift.Name,
                        IsActive = sd.IsActive,
                        Notes = sd.Notes
                    }).ToList(),
                ScheduleType = s.ScheduleType,
                IsActive = s.IsActive,
                Notes = s.Notes,
                ExcludedDates = s.ExcludedDates,
                CreatedAt = s.CreatedAt,
                LastUpdatedAt = s.LastUpdatedAt
            })
            .ToListAsync(cancellationToken);

        return PaginatedResponse<AttendanceScheduleResponse>.Create(
            schedules,
            totalCount,
            query.Page,
            query.PageSize);
    }
}
