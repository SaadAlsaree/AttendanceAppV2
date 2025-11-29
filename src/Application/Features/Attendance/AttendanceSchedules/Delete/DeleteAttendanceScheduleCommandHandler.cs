using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Attendance;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Attendance.AttendanceSchedules.Delete;

internal sealed class DeleteAttendanceScheduleCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<DeleteAttendanceScheduleCommand, bool>
{
    public async Task<Result<bool>> Handle(DeleteAttendanceScheduleCommand command, CancellationToken cancellationToken)
    {
        AttendanceSchedule? schedule = await context.AttendanceSchedules
            .Include(s => s.Attendances)
            .SingleOrDefaultAsync(s => s.Id == command.AttendanceScheduleId, cancellationToken);

        if (schedule is null)
        {
            return Result.Failure<bool>(AttendanceScheduleErrors.NotFound(command.AttendanceScheduleId));
        }

        // Check if schedule has associated attendance records
        if (schedule.Attendances.Any())
        {
            return Result.Failure<bool>(AttendanceScheduleErrors.ScheduleAlreadyExists(schedule.EmployeeId, schedule.StartDate.ToDateTime(TimeOnly.MinValue)));
        }

        // Perform soft delete
        schedule.IsDeleted = true;
        schedule.DeletedAt = dateTimeProvider.GetUtcNow();

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}
