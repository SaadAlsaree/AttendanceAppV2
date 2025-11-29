using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Models;
using Domain.Entities.Attendance;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Attendance.GetById;

internal sealed class GetAttendanceByIdQueryHandler(
    IApplicationDbContext context,
    IHasPermission hasPermission,
    IUserContext userContext)
    : IQueryHandler<GetAttendanceByIdQuery, ApiResponse<AttendanceResponse>>
{
    public async Task<Result<ApiResponse<AttendanceResponse>>> Handle(GetAttendanceByIdQuery query, CancellationToken cancellationToken)
    {
        Domain.Entities.Attendance.Attendance? attendance = await context.Attendances
            .Where(a => a.CheckInTime.HasValue || a.CheckOutTime.HasValue)
            .Include(a => a.Employee)
            .Include(a => a.Shift)
            .AsNoTracking()
            .SingleOrDefaultAsync(a => a.Id == query.AttendanceId, cancellationToken);

        if (attendance is null)
        {
            return Result.Failure<ApiResponse<AttendanceResponse>>(AttendanceErrors.NotFound(query.AttendanceId));
        }

        // check if user role not Admin then apply accessible unit ids filter
        UserInfoDto user = await userContext.GetUserAsync();
        if (user.Role != Role.Admin)
        {
            IEnumerable<Guid> accessibleUnitIds = await hasPermission.GetAccessibleUnitIdsAsync(cancellationToken);
            if (!accessibleUnitIds.Contains(attendance.Employee.OrganizationalUnitId!.Value))
            {
                return Result.Failure<ApiResponse<AttendanceResponse>>(AttendanceErrors.NotFound(query.AttendanceId));
            }
        }

        var response = new AttendanceResponse
        {
            Id = attendance.Id,
            EmployeeId = attendance.EmployeeId,
            OrganizationId = attendance.OrganizationId,
            Date = attendance.Date,
            CheckInTime = attendance.CheckInTime,
            CheckOutTime = attendance.CheckOutTime,
            Status = attendance.Status,
            ShiftId = attendance.ShiftId,
            WorkingMinutes = attendance.WorkingMinutes,
            BreakMinutes = attendance.BreakMinutes,
            OvertimeMinutes = attendance.OvertimeMinutes,
            LateMinutes = attendance.LateMinutes,
            EarlyLeaveMinutes = attendance.EarlyLeaveMinutes,
            Notes = attendance.Notes,
            ApprovedBy = attendance.ApprovedBy,
            ApprovedAt = attendance.ApprovedAt,
            AttendanceScheduleId = attendance.AttendanceScheduleId,
            CreatedAt = attendance.CreatedAt,
            UpdatedAt = attendance.LastUpdatedAt,
            FullName = attendance.Employee.FullName,
            Code = attendance.Employee.Code,
            ShiftName = attendance.Shift?.Name,

        };

        return Result.Success(ApiResponse<AttendanceResponse>.Success(response));
    }
}
