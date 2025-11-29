using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Features.Attendance.AttendanceSchedules.shared;
using Application.Models;
using Domain.Entities.Attendance;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Attendance.AttendanceSchedules.GetById;

internal sealed class GetAttendanceScheduleByIdQueryHandler(
    IApplicationDbContext context,
    IHasPermission hasPermission,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetAttendanceScheduleByIdQuery, ApiResponse<AttendanceScheduleResponse>>
{
    public async Task<Result<ApiResponse<AttendanceScheduleResponse>>> Handle(GetAttendanceScheduleByIdQuery query, CancellationToken cancellationToken)
    {
        AttendanceSchedule? schedule = await context.AttendanceSchedules
            .Include(s => s.Employee)
                .ThenInclude(e => e.OrganizationalUnit)
            .Include(s => s.ScheduleDays)
                .ThenInclude(sd => sd.Shift)
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.Id == query.AttendanceScheduleId, cancellationToken);

        if (schedule is null)
        {
            return Result.Failure<ApiResponse<AttendanceScheduleResponse>>(AttendanceScheduleErrors.NotFound(query.AttendanceScheduleId));
        }

        // check if user role not Admin then apply accessible unit ids filter
        UserInfoDto user = await userContext.GetUserAsync();
        if (user.Role != Role.Admin)
        {
            IEnumerable<Guid> accessibleUnitIds = await hasPermission.GetAccessibleUnitIdsAsync(cancellationToken);
            if (!accessibleUnitIds.Contains(schedule.Employee.OrganizationalUnitId!.Value))
            {
                return Result.Failure<ApiResponse<AttendanceScheduleResponse>>(AttendanceScheduleErrors.NotFound(query.AttendanceScheduleId));
            }
        }

        // حساب نطاق التاريخ: من اليوم إلى 10 أيام قادمة
        var today = DateOnly.FromDateTime(dateTimeProvider.GetCurrentLocalTime().Date);
        DateOnly endDate = today.AddDays(10);

        // تصفية الأيام المستثناة لتشمل فقط الأيام في النطاق المحدد
        var filteredExcludedDates = schedule.ExcludedDates
            .Where(date => date >= today && date <= endDate)
            .OrderBy(date => date)
            .ToList();

        var response = new AttendanceScheduleResponse
        {
            Id = schedule.Id,
            EmployeeId = schedule.EmployeeId,
            EmployeeName = schedule.Employee.FullName,
            EmployeeEmail = schedule.Employee.Email,
            EmployeeOrganizationId = schedule.Employee.OrganizationalUnitId ?? Guid.Empty,
            EmployeeOrganizationName = schedule.Employee.OrganizationalUnit?.UnitName ?? string.Empty,
            StartDate = schedule.StartDate,
            EndDate = schedule.EndDate,
            ScheduleType = schedule.ScheduleType,
            IsActive = schedule.IsActive,
            Notes = schedule.Notes,
            ExcludedDates = filteredExcludedDates,
            CreatedAt = schedule.CreatedAt,
            LastUpdatedAt = schedule.LastUpdatedAt,
            ScheduleDays = schedule.ScheduleDays
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
                }).ToList()
        };

        return Result.Success(new ApiResponse<AttendanceScheduleResponse>
        {
            Data = response,
            Message = "Attendance schedule retrieved successfully",
            IsSuccess = true
        });
    }
}
