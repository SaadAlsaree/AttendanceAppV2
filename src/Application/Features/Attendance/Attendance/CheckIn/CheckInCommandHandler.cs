using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Attendance.Shared;
using Domain.Entities.Attendance;
using Domain.Entities.Organizations;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using AttendanceEntity = Domain.Entities.Attendance.Attendance;

namespace Application.Attendance.CheckIn;

internal sealed class CheckInCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider,
    IAttendanceCalculationService calculationService)
    : ICommandHandler<CheckInCommand, AttendanceResponse>
{
    public async Task<Result<AttendanceResponse>> Handle(CheckInCommand command, CancellationToken cancellationToken)
    {
        // Find existing attendance record for today
        var today = DateOnly.FromDateTime(dateTimeProvider.Now);
        var todayUtc = DateTime.SpecifyKind(today.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

        AttendanceEntity? attendance = await context.Attendances
            .Include(a => a.Employee)
            .Include(a => a.Shift)
            .SingleOrDefaultAsync(a =>
                a.EmployeeId == command.EmployeeId &&
                a.Date.Date == todayUtc,
                cancellationToken);

        // If no attendance record exists for today, create one
        if (attendance is null)
        {
            // Validate employee exists
            Employee? employee = await context.Employees
                .Include(e => e.OrganizationalUnit)
                .AsNoTracking()
                .SingleOrDefaultAsync(e => e.Id == command.EmployeeId, cancellationToken);

            if (employee is null)
            {
                return Result.Failure<AttendanceResponse>(EmployeeErrors.DuplicateEmployeeCheckIn(command.EmployeeId));
            }

            if (employee.OrganizationalUnit is null)
            {
                return Result.Failure<AttendanceResponse>(EmployeeErrors.NotFound(command.EmployeeId));
            }



            // Create new attendance record
            AttendanceSchedule? activeSchedule = await context.AttendanceSchedules
                .Include(s => s.ScheduleDays)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.IsActive && s.StartDate <= today && (!s.EndDate.HasValue || s.EndDate >= today) && s.EmployeeId == employee.Id, cancellationToken);

            // تحقق من وجود جدول ودوام
            if (activeSchedule is null)
            {
                return Result.Failure<AttendanceResponse>(AttendanceErrors.MissingScheduleOrShift(command.EmployeeId, today));
            }

            // get today's schedule day
            ScheduleDay? todayScheduleDay = await context.ScheduleDays
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.ScheduleDayDate == today && d.IsActive && d.AttendanceScheduleId == activeSchedule.Id, cancellationToken);

            if (todayScheduleDay is null)
            {
                return Result.Failure<AttendanceResponse>(AttendanceErrors.MissingScheduleOrShift(command.EmployeeId, today));
            }

            // Load the shift for calculation
            Shift? shift = await context.Shifts
                .AsNoTracking()
                .SingleOrDefaultAsync(s => s.Id == todayScheduleDay.ShiftId, cancellationToken);

            // Ensure DateTimeAttend is in UTC before saving to database
            DateTime dateTimeAttendUtc = dateTimeProvider.EnsureUtc(command.DateTimeAttend);

            attendance = new AttendanceEntity
            {
                EmployeeId = command.EmployeeId,
                OrganizationId = employee.OrganizationalUnit!.Id,
                Date = DateTime.SpecifyKind(today.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc),
                ShiftId = todayScheduleDay.ShiftId,
                AttendanceScheduleId = activeSchedule.Id,
                CheckInTime = dateTimeAttendUtc,
                Notes = command.Notes,
                Status = AttendanceStatus.Present, // Will be updated below based on calculation
                CreatedAt = dateTimeProvider.GetUtcNow()
            };

            context.Attendances.Add(attendance);

            // Calculate late minutes and update status for new attendance record
            if (shift is not null)
            {
                // For check-in, we need to estimate check-out time to calculate metrics
                // We'll use the shift end time as an estimate
                DateTime estimatedCheckOutTime = attendance.Date.Date.Add(shift.EndTime.ToTimeSpan());

                // Get approved leaves for this employee on this date
                List<AttendanceBreak> approvedLeaves = await context.AttendanceBreaks
                    .Where(b => b.AttendanceId == attendance.Id &&
                               b.BreakType == BreakType.Vacation &&
                               b.StartTime.Date == attendance.Date.Date)
                    .ToListAsync(cancellationToken);

                AttendanceMetrics metrics = approvedLeaves.Any()
                    ? calculationService.CalculateMetricsWithLeave(command.DateTimeAttend, estimatedCheckOutTime, shift, approvedLeaves)
                    : calculationService.CalculateMetrics(command.DateTimeAttend, estimatedCheckOutTime, shift);

                attendance.LateMinutes = metrics.LateMinutes;

                // Update status based on lateness and shift configuration
                attendance.Status = DetermineAttendanceStatus(metrics, shift, command.DateTimeAttend);
            }
        }
        else
        {
            // Check if already checked in
            if (attendance.CheckInTime.HasValue)
            {
                return Result.Failure<AttendanceResponse>(AttendanceErrors.AlreadyCheckedIn(attendance.EmployeeId));
            }

            // Check if attendance is approved (cannot modify approved records)
            if (attendance.ApprovedBy.HasValue)
            {
                return Result.Failure<AttendanceResponse>(AttendanceErrors.AlreadyApproved(attendance.Id));
            }

            // Update existing attendance record with check-in information
            // Ensure DateTimeAttend is in UTC before saving to database
            DateTime dateTimeAttendUtc = dateTimeProvider.EnsureUtc(command.DateTimeAttend);
            attendance.CheckInTime = dateTimeAttendUtc;
            attendance.Notes = command.Notes;
            attendance.LastUpdatedAt = dateTimeProvider.GetUtcNow();

            // Calculate late minutes if shift is assigned
            if (attendance.Shift is not null)
            {
                // For check-in, we need to estimate check-out time to calculate metrics
                // We'll use the shift end time as an estimate
                DateTime estimatedCheckOutTime = attendance.Date.Date.Add(attendance.Shift.EndTime.ToTimeSpan());

                // Get approved leaves for this employee on this date
                List<AttendanceBreak> approvedLeaves = await context.AttendanceBreaks
                    .Where(b => b.AttendanceId == attendance.Id &&
                               b.BreakType == BreakType.Vacation &&
                               b.StartTime.Date == attendance.Date.Date)
                    .ToListAsync(cancellationToken);

                AttendanceMetrics metrics = approvedLeaves.Any()
                    ? calculationService.CalculateMetricsWithLeave(command.DateTimeAttend, estimatedCheckOutTime, attendance.Shift, approvedLeaves)
                    : calculationService.CalculateMetrics(command.DateTimeAttend, estimatedCheckOutTime, attendance.Shift);

                attendance.LateMinutes = metrics.LateMinutes;

                // Update status based on lateness and shift configuration
                attendance.Status = DetermineAttendanceStatus(metrics, attendance.Shift, command.DateTimeAttend);
            }
        }

        // Create attendance log entry for check-in
        // Ensure DateTimeAttend is in UTC before saving to database
        DateTime logDateTimeAttendUtc = dateTimeProvider.EnsureUtc(command.DateTimeAttend);
        var attendanceLog = new AttendanceLog
        {
            DateTimeAttend = logDateTimeAttendUtc,
            CardNo = command.CardNo,
            EmpID = command.EmployeeId.ToString(),
            DateWork = today,
            TimeAttend = logDateTimeAttendUtc.TimeOfDay,
            Direct = 1,
            DeviceName = command.DeviceName,
            DeviceNo = command.DeviceNo,
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

    private static AttendanceStatus DetermineAttendanceStatus(AttendanceMetrics metrics, Shift shift, DateTime checkInTime)
    {
        // Check if employee is very late (beyond max late minutes if configured)
        if (shift.MaxLateMinutes.HasValue && metrics.LateMinutes > shift.MaxLateMinutes.Value)
        {
            return AttendanceStatus.Absent;
        }

        // Check if employee is late
        if (metrics.LateMinutes > 0)
        {
            return AttendanceStatus.Late;
        }

        // Check if employee checked in early (if not allowed)
        var checkInTimeOnly = TimeOnly.FromDateTime(checkInTime);
        if (!shift.AllowEarlyCheckIn && checkInTimeOnly < shift.StartTime)
        {
            return AttendanceStatus.Late; // Treat early check-in as late if not allowed
        }

        // Default to present
        return AttendanceStatus.Present;
    }
}
