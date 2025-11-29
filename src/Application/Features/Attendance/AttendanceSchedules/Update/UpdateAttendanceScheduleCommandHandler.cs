using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Features.Attendance.AttendanceSchedules.shared;
using Domain.Entities.Attendance;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Attendance.AttendanceSchedules.Update;

internal sealed class UpdateAttendanceScheduleCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<UpdateAttendanceScheduleCommand, AttendanceScheduleResponse>
{
    public async Task<Result<AttendanceScheduleResponse>> Handle(UpdateAttendanceScheduleCommand command, CancellationToken cancellationToken)
    {
        AttendanceSchedule? schedule = await context.AttendanceSchedules
            .Include(s => s.Employee)
            .Include(s => s.ScheduleDays)
            .ThenInclude(sd => sd.Shift)
            .SingleOrDefaultAsync(s => s.Id == command.AttendanceScheduleId, cancellationToken);

        if (schedule is null)
        {
            return Result.Failure<AttendanceScheduleResponse>(AttendanceScheduleErrors.NotFound(command.AttendanceScheduleId));
        }

        // Validate date range if both dates are provided
        if (command.StartDate.HasValue && command.EndDate.HasValue && command.StartDate.Value >= command.EndDate.Value)
        {
            return Result.Failure<AttendanceScheduleResponse>(AttendanceScheduleErrors.InvalidDateRange(command.StartDate.Value.ToDateTime(TimeOnly.MinValue), command.EndDate.Value.ToDateTime(TimeOnly.MinValue)));
        }

        // Update basic properties
        UpdateBasicProperties(schedule, command, dateTimeProvider);

        // Update ScheduleDays only if explicitly requested
        if (command.UpdateScheduleDays)
        {
            await UpdateScheduleDaysAsync(schedule, command.ScheduleDays ?? [], context, dateTimeProvider, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);



        return CreateResponse(schedule);
    }

    private static void UpdateBasicProperties(AttendanceSchedule schedule, UpdateAttendanceScheduleCommand command, IDateTimeProvider dateTimeProvider)
    {
        if (command.StartDate.HasValue)
        {
            schedule.StartDate = command.StartDate.Value;
        }

        if (command.EndDate.HasValue)
        {
            schedule.EndDate = command.EndDate.Value;
        }

        schedule.ScheduleType = command.ScheduleType;

        if (command.IsActive.HasValue)
        {
            schedule.IsActive = command.IsActive.Value;
        }

        if (command.Notes is not null)
        {
            schedule.Notes = command.Notes;
        }

        if (command.ExcludedDates is not null)
        {
            schedule.ExcludedDates = command.ExcludedDates;
        }

        schedule.LastUpdatedAt = dateTimeProvider.GetUtcNow();
    }

    private static async Task UpdateScheduleDaysAsync(
        AttendanceSchedule schedule,
        List<UpdateScheduleDayCommand> scheduleDaysCommand,
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider,
        CancellationToken cancellationToken)
    {
        // Validate all shift IDs exist before making any changes
        var shiftIds = scheduleDaysCommand
            .Where(sd => sd.ShiftId.HasValue)
            .Select(sd => sd.ShiftId!.Value)
            .Distinct()
            .ToList();

        if (shiftIds.Count > 0)
        {
            List<Guid> existingShiftIds = await context.Shifts
                .Where(s => shiftIds.Contains(s.Id))
                .Select(s => s.Id)
                .ToListAsync(cancellationToken);

            var invalidShiftIds = shiftIds.Except(existingShiftIds).ToList();
            if (invalidShiftIds.Count > 0)
            {
                throw new InvalidOperationException($"The following shift IDs do not exist: {string.Join(", ", invalidShiftIds)}");
            }
        }

        // Get existing schedule day IDs that should be kept
        var scheduleDayIdsToKeep = scheduleDaysCommand
            .Where(sd => sd.Id.HasValue)
            .Select(sd => sd.Id!.Value)
            .ToHashSet();

        // Remove schedule days that are not in the update list
        var scheduleDaysToRemove = schedule.ScheduleDays
            .Where(sd => !scheduleDayIdsToKeep.Contains(sd.Id))
            .ToList();

        foreach (ScheduleDay dayToRemove in scheduleDaysToRemove)
        {
            schedule.ScheduleDays.Remove(dayToRemove);
        }

        // Process schedule days from the command
        foreach (UpdateScheduleDayCommand scheduleDayCommand in scheduleDaysCommand)
        {
            if (scheduleDayCommand.Id.HasValue)
            {
                // Update existing schedule day
                ScheduleDay? existingDay = schedule.ScheduleDays.FirstOrDefault(sd => sd.Id == scheduleDayCommand.Id.Value);
                if (existingDay is not null)
                {
                    UpdateExistingScheduleDay(existingDay, scheduleDayCommand, dateTimeProvider);
                }
            }
            else
            {
                // Add new schedule day
                ScheduleDay newScheduleDay = CreateNewScheduleDay(schedule.Id, scheduleDayCommand, dateTimeProvider);
                schedule.ScheduleDays.Add(newScheduleDay);
            }
        }
    }

    private static void UpdateExistingScheduleDay(ScheduleDay existingDay, UpdateScheduleDayCommand command, IDateTimeProvider dateTimeProvider)
    {
        if (command.ScheduleDayDate.HasValue)
        {
            existingDay.ScheduleDayDate = command.ScheduleDayDate.Value;
        }

        if (command.ShiftId.HasValue)
        {
            existingDay.ShiftId = command.ShiftId.Value;
        }

        if (command.IsActive.HasValue)
        {
            existingDay.IsActive = command.IsActive.Value;
        }

        if (command.Notes is not null)
        {
            existingDay.Notes = command.Notes;
        }

        existingDay.LastUpdatedAt = dateTimeProvider.GetUtcNow();
    }

    private static ScheduleDay CreateNewScheduleDay(Guid scheduleId, UpdateScheduleDayCommand command, IDateTimeProvider dateTimeProvider)
    {
        return new ScheduleDay
        {
            AttendanceScheduleId = scheduleId,
            ScheduleDayDate = command.ScheduleDayDate ?? throw new InvalidOperationException("ScheduleDayDate is required for new ScheduleDays."),
            ShiftId = command.ShiftId ?? throw new InvalidOperationException("ShiftId is required for new ScheduleDays."),
            IsActive = command.IsActive ?? true,
            Notes = command.Notes,
            CreatedAt = dateTimeProvider.GetUtcNow(),
            IsDeleted = false
        };
    }

    private static AttendanceScheduleResponse CreateResponse(AttendanceSchedule schedule)
    {
        return new AttendanceScheduleResponse
        {
            Id = schedule.Id,
            EmployeeId = schedule.EmployeeId,
            EmployeeName = schedule.Employee.FullName,
            StartDate = schedule.StartDate,
            EndDate = schedule.EndDate,
            ScheduleType = schedule.ScheduleType,
            IsActive = schedule.IsActive,
            Notes = schedule.Notes,
            ExcludedDates = schedule.ExcludedDates,
            CreatedAt = schedule.CreatedAt,
            LastUpdatedAt = schedule.LastUpdatedAt,
            ScheduleDays = schedule.ScheduleDays
                .OrderBy(sd => sd.ScheduleDayDate)
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
    }
}
