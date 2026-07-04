using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Attendance.Shared;
using Domain.Entities.Attendance;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using AttendanceEntity = Domain.Entities.Attendance.Attendance;

namespace Application.Attendance.CheckOut;

internal sealed class CheckOutCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider,
    IAttendanceCalculationService calculationService)
    : ICommandHandler<CheckOutCommand, AttendanceResponse>
{
    public async Task<Result<AttendanceResponse>> Handle(CheckOutCommand command, CancellationToken cancellationToken)
    {
        // Find existing attendance record for today
        var today = DateOnly.FromDateTime(dateTimeProvider.GetUtcNow());
        var todayUtc = DateTime.SpecifyKind(today.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

        AttendanceEntity? attendance = await context.Attendances
            .Include(a => a.Employee)
            .Include(a => a.Shift)
            .SingleOrDefaultAsync(a =>
                a.EmployeeId == command.EmployeeId &&
                a.Date == todayUtc,
                cancellationToken);

        if (attendance is null)
        {
            return Result.Failure<AttendanceResponse>(AttendanceErrors.NotFound(command.AttendanceId));
        }

        // Check if already checked out
        if (attendance.CheckOutTime.HasValue)
        {
            return Result.Failure<AttendanceResponse>(AttendanceErrors.AlreadyCheckedOut(attendance.Id));
        }

        // Check if attendance is approved (cannot modify approved records)
        //if (attendance.ApprovedBy.HasValue)
        //{
        //    return Result.Failure<AttendanceResponse>(AttendanceErrors.AlreadyApproved(attendance.Id));
        //}

        // Ensure CheckOutTime is in UTC before saving to database
        DateTime checkOutTimeUtc = dateTimeProvider.EnsureUtc(command.CheckOutTime);

        // Validate check-out time is after check-in time
        if (attendance.CheckInTime.HasValue && checkOutTimeUtc <= attendance.CheckInTime.Value)
        {
            return Result.Failure<AttendanceResponse>(AttendanceErrors.AlreadyCheckedOut(attendance.Id));
        }

        // Update attendance record with check-out information
        attendance.CheckOutTime = checkOutTimeUtc;

        if (command.Notes is not null)
        {
            attendance.Notes = command.Notes;
        }

        attendance.LastUpdatedAt = dateTimeProvider.GetUtcNow();

        // ساعات العمل تُحسب من الوقتين فقط — بدون الحاجة إلى وردية
        if (attendance.CheckInTime.HasValue)
        {
            attendance.WorkingMinutes = calculationService.CalculateWorkingMinutes(
                attendance.CheckInTime.Value,
                checkOutTimeUtc);
        }

        // Calculate shift-dependent metrics if shift is assigned and check-in time exists
        if (attendance.Shift is not null && attendance.CheckInTime.HasValue)
        {
            AttendanceMetrics metrics = calculationService.CalculateMetrics(
                attendance.CheckInTime.Value,
                checkOutTimeUtc,
                attendance.Shift);

            attendance.LateMinutes = metrics.LateMinutes;
            attendance.EarlyLeaveMinutes = metrics.EarlyLeaveMinutes;
            attendance.OvertimeMinutes = metrics.OvertimeMinutes;

            // Update status based on metrics
            attendance.Status = (metrics.EarlyLeaveMinutes, metrics.OvertimeMinutes, metrics.LateMinutes) switch
            {
                ( > 0, _, _) => AttendanceStatus.Early_Out,
                (_, > 0, _) => AttendanceStatus.Overtime,
                (_, _, > 0) => AttendanceStatus.Late,
                _ => AttendanceStatus.Present
            };
        }

        // Create attendance log entry for check-out
        var attendanceLog = new AttendanceLog
        {
            DateTimeAttend = checkOutTimeUtc,
            CardNo = command.CardNo,
            EmpID = command.EmployeeId.ToString(),
            DateWork = today,
            TimeAttend = checkOutTimeUtc.TimeOfDay,
            Direct = 2,
            CreatedAt = dateTimeProvider.GetUtcNow()
        };

        context.AttendanceLogs.Add(attendanceLog);

        await context.SaveChangesAsync(cancellationToken);

        // Reload attendance with navigation properties for response
        attendance = await context.Attendances
            .Include(a => a.Employee)
            .Include(a => a.Shift)
            .AsNoTracking()
            .SingleAsync(a => a.Id == attendance.Id, cancellationToken);

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
            ApproverName = null // Would need to join with Users table for approver name
        };

        return response;
    }
}
