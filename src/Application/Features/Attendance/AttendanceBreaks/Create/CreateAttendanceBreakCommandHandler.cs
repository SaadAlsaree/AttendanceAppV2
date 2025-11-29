using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Attendance;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using AttendanceEntity = Domain.Entities.Attendance.Attendance;

namespace Application.Attendance.AttendanceBreaks.Create;

internal sealed class CreateAttendanceBreakCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<CreateAttendanceBreakCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateAttendanceBreakCommand command, CancellationToken cancellationToken)
    {
        // Check if attendance record exists
        AttendanceEntity? attendance = await context.Attendances
            .AsNoTracking()
            .SingleOrDefaultAsync(a => a.Id == command.AttendanceId, cancellationToken);

        if (attendance is null)
        {
            return Result.Failure<Guid>(AttendanceBreakErrors.AttendanceNotFound(command.AttendanceId));
        }

        // Validate start time is not in the future
        if (command.StartTime > dateTimeProvider.GetUtcNow())
        {
            return Result.Failure<Guid>(AttendanceBreakErrors.BreakTimeInFuture(command.StartTime));
        }

        // Validate start time is within attendance period
        if (command.StartTime.Date != attendance.Date.Date)
        {
            return Result.Failure<Guid>(AttendanceBreakErrors.BreakOutsideAttendancePeriod(command.StartTime, attendance.Date));
        }

        // Calculate end time if duration is provided
        DateTime endTime = command.EndTime ?? command.StartTime.AddMinutes(command.DurationMinutes ?? 0);

        // Validate end time is after start time
        if (endTime <= command.StartTime)
        {
            return Result.Failure<Guid>(AttendanceBreakErrors.InvalidBreakTime(command.StartTime, endTime));
        }

        // Check for overlapping breaks
        bool hasOverlappingBreaks = await context.AttendanceBreaks
            .AsNoTracking()
            .AnyAsync(b =>
                b.AttendanceId == command.AttendanceId &&
                (b.StartTime <= command.StartTime && b.EndTime > command.StartTime ||
                 b.StartTime < endTime && b.EndTime >= endTime ||
                 b.StartTime >= command.StartTime && b.EndTime <= endTime),
                cancellationToken);

        if (hasOverlappingBreaks)
        {
            return Result.Failure<Guid>(AttendanceBreakErrors.OverlappingBreaks(command.AttendanceId, command.StartTime, endTime));
        }

        // Validate break duration
        int durationMinutes = (int)(endTime - command.StartTime).TotalMinutes;
        if (durationMinutes < 1 || durationMinutes > 480)
        {
            return Result.Failure<Guid>(AttendanceBreakErrors.InvalidBreakDuration(durationMinutes));
        }

        var attendanceBreak = new AttendanceBreak
        {
            AttendanceId = command.AttendanceId,
            StartTime = command.StartTime,
            EndTime = endTime,
            DurationMinutes = durationMinutes,
            BreakType = command.BreakType,
            Notes = command.Notes,
            CreatedAt = dateTimeProvider.GetUtcNow()
        };

        context.AttendanceBreaks.Add(attendanceBreak);

        await context.SaveChangesAsync(cancellationToken);

        return attendanceBreak.Id;
    }
}
