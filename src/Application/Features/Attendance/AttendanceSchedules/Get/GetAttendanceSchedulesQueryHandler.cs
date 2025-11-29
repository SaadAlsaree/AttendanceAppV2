using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Features.Attendance.AttendanceSchedules.shared;
using Application.Models;
using Domain.Entities.Attendance;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using SharedKernel;

namespace Application.Attendance.AttendanceSchedules.Get;

internal sealed class GetAttendanceSchedulesQueryHandler(
    IApplicationDbContext context,
    IHasPermission hasPermission,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext)
    : IQueryHandler<GetAttendanceSchedulesQuery, PaginatedResponse<AttendanceScheduleResponse>>
{
    public async Task<Result<PaginatedResponse<AttendanceScheduleResponse>>> Handle(GetAttendanceSchedulesQuery query, CancellationToken cancellationToken)
    {
        IQueryable<AttendanceSchedule> queryable = context.AttendanceSchedules
            .Include(s => s.Employee)
            .Include(s => s.ScheduleDays)
            .ThenInclude(sd => sd.Shift)
            .AsNoTracking();

        // check if user role not Admin then apply accessible unit ids filter
        UserInfoDto user = await userContext.GetUserAsync();
        if (user.Role != Role.Admin)
        {
            IEnumerable<Guid> accessibleUnitIds = await hasPermission.GetAccessibleUnitIdsAsync(cancellationToken);
            queryable = queryable.Where(s => accessibleUnitIds.Contains(s.Employee.OrganizationalUnitId!.Value));
        }

        // Apply filters
        if (query.EmployeeId.HasValue)
        {
            queryable = queryable.Where(s => s.EmployeeId == query.EmployeeId.Value);
        }

        if (query.ScheduleType.HasValue)
        {
            queryable = queryable.Where(s => s.ScheduleType == query.ScheduleType.Value);
        }

        if (query.IsActive.HasValue)
        {
            queryable = queryable.Where(s => s.IsActive == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            queryable = queryable.Where(s =>
                s.Employee.FullName.Contains(query.SearchTerm) ||
                s.Notes!.Contains(query.SearchTerm));
        }

        // Apply sorting
        queryable = query.SortBy switch
        {
            "employeeName" => query.SortOrder == SortOrder.Descending
                ? queryable.OrderByDescending(s => s.Employee.FirstName)
                : queryable.OrderBy(s => s.Employee.FirstName),
            "startDate" => query.SortOrder == SortOrder.Descending
                ? queryable.OrderByDescending(s => s.StartDate)
                : queryable.OrderBy(s => s.StartDate),
            "scheduleType" => query.SortOrder == SortOrder.Descending
                ? queryable.OrderByDescending(s => s.ScheduleType)
                : queryable.OrderBy(s => s.ScheduleType),
            "isActive" => query.SortOrder == SortOrder.Descending
                ? queryable.OrderByDescending(s => s.IsActive)
                : queryable.OrderBy(s => s.IsActive),
            "createdAt" => query.SortOrder == SortOrder.Descending
                ? queryable.OrderByDescending(s => s.CreatedAt)
                : queryable.OrderBy(s => s.CreatedAt),
            _ => queryable.OrderByDescending(s => s.CreatedAt)
        };

        int totalCount = await queryable.CountAsync(cancellationToken);


        // حساب نطاق التاريخ: من اليوم إلى 10 أيام قادمة
        var today = DateOnly.FromDateTime(dateTimeProvider.GetCurrentLocalTime().Date);
        
        List<AttendanceScheduleResponse> schedules = await queryable
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(s => new AttendanceScheduleResponse
            {
                Id = s.Id,
                EmployeeId = s.EmployeeId,
                EmployeeName = s.Employee.FullName,
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                ScheduleType = s.ScheduleType,
                ScheduleTypeName = GetDisplayName(s.ScheduleType),
                IsActive = s.IsActive,
                Notes = s.Notes,
                ExcludedDates = s.ExcludedDates,
                CreatedAt = s.CreatedAt,
                LastUpdatedAt = s.LastUpdatedAt,
                ScheduleDays = s.ScheduleDays
                    .Where(sd => sd.ScheduleDayDate >= today)
                    .OrderBy(sd => sd.ScheduleDayDate)
                    .Take(10)
                    .Select(sd => new ScheduleDayResponse
                    {
                        Id = sd.Id,
                        AttendanceScheduleId = sd.AttendanceScheduleId,
                        ScheduleDayDate = sd.ScheduleDayDate,
                        ShiftId = sd.ShiftId,
                        ShiftName = sd.Shift.Name,
                        IsActive = sd.IsActive,
                        Notes = sd.Notes
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<AttendanceScheduleResponse>
        {
            Data = schedules,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling((double)totalCount / query.PageSize)
        };
    }

    private static string GetDisplayName(Domain.Enums.ScheduleType scheduleType)
    {
        FieldInfo? field = scheduleType.GetType().GetField(scheduleType.ToString());
        DisplayAttribute? displayAttribute = field?.GetCustomAttribute<DisplayAttribute>();
        return displayAttribute?.Name ?? scheduleType.ToString();
    }
}
