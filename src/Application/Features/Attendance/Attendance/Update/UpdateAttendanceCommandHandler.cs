using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Attendance.Shared;
using Domain.Entities.Attendance;
using Domain.Entities.Organizations;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Attendance.Update;

internal sealed class UpdateAttendanceCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider,
    IAttendanceCalculationService calculationService,
    IHasPermission hasPermission)
    : ICommandHandler<UpdateAttendanceCommand, AttendanceResponse>
{
    public async Task<Result<AttendanceResponse>> Handle(UpdateAttendanceCommand command, CancellationToken cancellationToken)
    {
        Domain.Entities.Attendance.Attendance? attendance = await context.Attendances
            .Include(a => a.Employee)
            .Include(a => a.Shift)
            .SingleOrDefaultAsync(a => a.Id == command.AttendanceId, cancellationToken);

        if (attendance is null)
        {
            return Result.Failure<AttendanceResponse>(AttendanceErrors.NotFound(command.AttendanceId));
        }

        // Scoped roles (e.g. OrgSupervisor) may only manage employees in their own unit tree.
        if (!await hasPermission.CanManageEmployeeAsync(attendance.EmployeeId, cancellationToken))
        {
            return Result.Failure<AttendanceResponse>(EmployeeErrors.AccessDenied);
        }

        // Cannot update approved attendance records without proper authorization
        if (attendance.ApprovedBy.HasValue)
        {
            return Result.Failure<AttendanceResponse>(AttendanceErrors.AlreadyApproved(command.AttendanceId));
        }

        // Validate check-in and check-out times are logical
        if (command.CheckInTime.HasValue && command.CheckOutTime.HasValue && command.CheckInTime.Value >= command.CheckOutTime.Value)
        {
            return Result.Failure<AttendanceResponse>(AttendanceErrors.AlreadyCheckedOut(command.AttendanceId));
        }

        // Update attendance properties
        if (command.CheckInTime.HasValue)
        {
            attendance.CheckInTime = command.CheckInTime.Value;
        }

        if (command.CheckOutTime.HasValue)
        {
            attendance.CheckOutTime = command.CheckOutTime.Value;
        }

        if (command.Notes is not null)
        {
            attendance.Notes = command.Notes;
        }

        if (command.Status.HasValue)
        {
            attendance.Status = command.Status.Value;
        }

        attendance.LastUpdatedAt = dateTimeProvider.GetUtcNow();

        // ساعات العمل تُحسب من الوقتين فقط — بدون الحاجة إلى وردية
        if (attendance.CheckInTime.HasValue && attendance.CheckOutTime.HasValue)
        {
            attendance.WorkingMinutes = calculationService.CalculateWorkingMinutes(
                attendance.CheckInTime.Value,
                attendance.CheckOutTime.Value);
        }

        // Recalculate shift-dependent metrics if both times are available and shift is assigned
        if (attendance.CheckInTime.HasValue && attendance.CheckOutTime.HasValue && attendance.Shift is not null)
        {
            AttendanceMetrics metrics = calculationService.CalculateMetrics(
                attendance.CheckInTime.Value,
                attendance.CheckOutTime.Value,
                attendance.Shift);

            attendance.LateMinutes = metrics.LateMinutes;
            attendance.EarlyLeaveMinutes = metrics.EarlyLeaveMinutes;
            attendance.OvertimeMinutes = metrics.OvertimeMinutes;

            // Update status based on metrics if not manually set
            if (!command.Status.HasValue)
            {
                attendance.Status = metrics switch
                {
                    { EarlyLeaveMinutes: > 0 } => AttendanceStatus.Early_Out,
                    { OvertimeMinutes: > 0 } => AttendanceStatus.Overtime,
                    { LateMinutes: > 0 } => AttendanceStatus.Late,
                    _ => AttendanceStatus.Present
                };
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        // Reload attendance with navigation properties for response
        attendance = await context.Attendances
            .Include(a => a.Employee)
            .Include(a => a.Shift)
            .AsNoTracking()
            .SingleAsync(a => a.Id == command.AttendanceId, cancellationToken);

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
            CheckInMethod = attendance.CheckInMethod,
            CheckOutMethod = attendance.CheckOutMethod,
            ApprovedBy = attendance.ApprovedBy,
            ApprovedAt = attendance.ApprovedAt,
            AttendanceScheduleId = attendance.AttendanceScheduleId,
            CreatedAt = attendance.CreatedAt,
            UpdatedAt = attendance.LastUpdatedAt,
            FullName = attendance.Employee.FullName,
            Code = attendance.Employee.Code,
            ShiftName = attendance.Shift?.Name,
            ApproverName = null // Would need to join with Users table for approver name
        };

        return response;
    }
}
