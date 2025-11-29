using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Attendance;
using Domain.Entities.Organizations;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Attendance.AttendanceSchedules.UpdateScheduleDays;

internal sealed class UpdateScheduleDaysCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<UpdateScheduleDaysCommand, bool>
{
    public async Task<Result<bool>> Handle(UpdateScheduleDaysCommand command, CancellationToken cancellationToken)
    {
        // Check if attendance schedule exists
        AttendanceSchedule? schedule = await context.AttendanceSchedules
            .Include(s => s.ScheduleDays)
            .SingleOrDefaultAsync(s => s.Id == command.AttendanceScheduleId, cancellationToken);

        if (schedule is null)
        {
            return Result.Failure<bool>(AttendanceScheduleErrors.NotFound(command.AttendanceScheduleId));
        }

        // Validate that all schedule day IDs belong to this schedule
        var scheduleDayIds = command.ScheduleDays.Select(sd => sd.Id).ToList();
        List<ScheduleDay> existingScheduleDays = await context.ScheduleDays
            .Where(sd => scheduleDayIds.Contains(sd.Id) && sd.AttendanceScheduleId == command.AttendanceScheduleId)
            .ToListAsync(cancellationToken);

        if (existingScheduleDays.Count != command.ScheduleDays.Count)
        {
            return Result.Failure<bool>(ScheduleDayErrors.InvalidScheduleDay(Guid.Empty));
        }

        // Check if all shifts exist
        var shiftIds = command.ScheduleDays.Select(sd => sd.ShiftId).Distinct().ToList();
        List<Guid> shiftsExist = await context.Shifts
            .AsNoTracking()
            .Where(s => shiftIds.Contains(s.Id))
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        if (shiftsExist.Count != shiftIds.Count)
        {
            Guid missingShiftId = shiftIds.FirstOrDefault(id => !shiftsExist.Contains(id));
            return Result.Failure<bool>(ShiftErrors.NotFound(missingShiftId));
        }

        // Get today's date for comparison
        var today = DateOnly.FromDateTime(dateTimeProvider.GetUtcNow().Date);

        // Update schedule days
        foreach (UpdateScheduleDayCommand scheduleDayCommand in command.ScheduleDays)
        {
            ScheduleDay? scheduleDay = existingScheduleDays.FirstOrDefault(sd => sd.Id == scheduleDayCommand.Id);

            if (scheduleDay is not null)
            {
                scheduleDay.ShiftId = scheduleDayCommand.ShiftId;
                scheduleDay.IsActive = scheduleDayCommand.IsActive;
                scheduleDay.Notes = scheduleDayCommand.Notes;
                scheduleDay.LastUpdatedAt = dateTimeProvider.GetUtcNow();

                // Update Attendance records if the schedule day is in the past or today
                if (scheduleDay.ScheduleDayDate <= today)
                {
                    // Find all Attendance records that match this schedule day
                    List<Domain.Entities.Attendance.Attendance> attendancesToUpdate = await context.Attendances
                        .Where(a => a.EmployeeId == schedule.EmployeeId &&
                                   DateOnly.FromDateTime(a.Date.Date) == scheduleDay.ScheduleDayDate &&
                                   (a.AttendanceScheduleId == null || a.AttendanceScheduleId == schedule.Id))
                        .ToListAsync(cancellationToken);

                    // Update ShiftId for all matching attendance records
                    foreach (Domain.Entities.Attendance.Attendance attendance in attendancesToUpdate)
                    {
                        attendance.ShiftId = scheduleDayCommand.ShiftId;
                        attendance.CheckInTime = null;
                        attendance.CheckOutTime = null;
                    }
                }
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}
