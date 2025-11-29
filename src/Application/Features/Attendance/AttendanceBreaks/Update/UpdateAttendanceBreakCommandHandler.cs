using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Attendance.AttendanceBreaks.Get;
using Domain.Entities.Attendance;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Attendance.AttendanceBreaks.Update;

internal sealed class UpdateAttendanceBreakCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<UpdateAttendanceBreakCommand, AttendanceBreakResponse>
{
    public async Task<Result<AttendanceBreakResponse>> Handle(UpdateAttendanceBreakCommand command, CancellationToken cancellationToken)
    {
        AttendanceBreak? attendanceBreak = await context.AttendanceBreaks
            .Include(b => b.Attendance)
            .ThenInclude(a => a.Employee)
            .SingleOrDefaultAsync(b => b.Id == command.AttendanceBreakId, cancellationToken);

        if (attendanceBreak is null)
        {
            return Result.Failure<AttendanceBreakResponse>(AttendanceBreakErrors.NotFound(command.AttendanceBreakId));
        }

        // Cannot update breaks for approved attendance records
        if (attendanceBreak.Attendance.ApprovedBy.HasValue)
        {
            return Result.Failure<AttendanceBreakResponse>(AttendanceErrors.AlreadyApproved(attendanceBreak.Attendance.Id));
        }

        // Update fields if provided
        if (command.BreakType.HasValue)
        {
            attendanceBreak.BreakType = command.BreakType.Value;
        }
        if (command.StartTime.HasValue)
        {
            attendanceBreak.StartTime = command.StartTime.Value;
        }
        if (command.EndTime.HasValue)
        {
            attendanceBreak.EndTime = command.EndTime.Value;
        }
        if (command.DurationMinutes.HasValue)
        {
            attendanceBreak.DurationMinutes = command.DurationMinutes.Value;
        }
        if (command.Notes is not null)
        {
            attendanceBreak.Notes = command.Notes;
        }

        // Validate times
        if (attendanceBreak.EndTime.HasValue && attendanceBreak.EndTime <= attendanceBreak.StartTime)
        {
            return Result.Failure<AttendanceBreakResponse>(AttendanceBreakErrors.InvalidBreakTime(attendanceBreak.StartTime, attendanceBreak.EndTime.Value));
        }

        // Validate break is within attendance period
        if (attendanceBreak.StartTime.Date != attendanceBreak.Attendance.Date.Date)
        {
            return Result.Failure<AttendanceBreakResponse>(AttendanceBreakErrors.BreakOutsideAttendancePeriod(attendanceBreak.StartTime, attendanceBreak.Attendance.Date));
        }

        // Validate duration
        int duration = attendanceBreak.EndTime.HasValue
            ? (int)(attendanceBreak.EndTime.Value - attendanceBreak.StartTime).TotalMinutes
            : attendanceBreak.DurationMinutes;
        if (duration < 1 || duration > 480)
        {
            return Result.Failure<AttendanceBreakResponse>(AttendanceBreakErrors.InvalidBreakDuration(duration));
        }
        attendanceBreak.DurationMinutes = duration;

        // Check for overlapping breaks
        bool hasOverlappingBreaks = await context.AttendanceBreaks
            .AsNoTracking()
            .AnyAsync(b =>
                b.Id != attendanceBreak.Id &&
                b.AttendanceId == attendanceBreak.AttendanceId &&
                (b.StartTime <= attendanceBreak.StartTime && b.EndTime > attendanceBreak.StartTime ||
                 b.StartTime < attendanceBreak.EndTime && b.EndTime >= attendanceBreak.EndTime ||
                 b.StartTime >= attendanceBreak.StartTime && b.EndTime <= attendanceBreak.EndTime),
                cancellationToken);
        if (hasOverlappingBreaks)
        {
            return Result.Failure<AttendanceBreakResponse>(AttendanceBreakErrors.OverlappingBreaks(attendanceBreak.AttendanceId, attendanceBreak.StartTime, attendanceBreak.EndTime ?? attendanceBreak.StartTime.AddMinutes(attendanceBreak.DurationMinutes)));
        }

        attendanceBreak.LastUpdatedAt = dateTimeProvider.GetUtcNow();
        await context.SaveChangesAsync(cancellationToken);

        var response = new AttendanceBreakResponse
        {
            Id = attendanceBreak.Id,
            AttendanceId = attendanceBreak.AttendanceId,
            EmployeeId = attendanceBreak.Attendance.EmployeeId,
            EmployeeName = attendanceBreak.Attendance.Employee.FirstName + " " + attendanceBreak.Attendance.Employee.FamilyName,
            StartTime = attendanceBreak.StartTime,
            EndTime = attendanceBreak.EndTime,
            DurationMinutes = attendanceBreak.DurationMinutes,
            BreakType = attendanceBreak.BreakType,
            Notes = attendanceBreak.Notes,
            CreatedAt = attendanceBreak.CreatedAt,
            LastUpdatedAt = attendanceBreak.LastUpdatedAt,
            AttendanceDate = attendanceBreak.Attendance.Date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
        };

        return response;
    }
}
