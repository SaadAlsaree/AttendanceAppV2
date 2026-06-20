using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Attendance;
using Domain.Entities.Organizations;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Attendance.AttendanceSchedules.Create;

internal sealed class CreateAttendanceScheduleCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<CreateAttendanceScheduleCommand, bool>
{
    public async Task<Result<bool>> Handle(CreateAttendanceScheduleCommand command, CancellationToken cancellationToken)
    {
        // Check if employee exists
        bool employeeExists = await context.Employees
            .AsNoTracking()
            .AnyAsync(e => e.Id == command.EmployeeId, cancellationToken);

        if (!employeeExists)
        {
            return Result.Failure<bool>(EmployeeErrors.NotFound(command.EmployeeId));
        }




        // Check for an active schedule whose date range OVERLAPS the requested range.
        // Two ranges [aStart, aEnd] and [bStart, bEnd] overlap iff aStart <= bEnd && bStart <= aEnd,
        // where a null EndDate means an open-ended (infinite) range.
        DateOnly? newEnd = command.EndDate;
        bool scheduleExists = await context.AttendanceSchedules
            .AsNoTracking()
            .AnyAsync(s =>
                s.EmployeeId == command.EmployeeId &&
                s.IsActive &&
                (newEnd == null || s.StartDate <= newEnd.Value) &&
                (s.EndDate == null || command.StartDate <= s.EndDate.Value),
                cancellationToken);

        if (scheduleExists)
        {
            return Result.Failure<bool>(AttendanceScheduleErrors.ScheduleAlreadyExists(command.EmployeeId, command.StartDate.ToDateTime(TimeOnly.MinValue)));
        }

        // Validate date range
        if (command.EndDate.HasValue && command.StartDate >= command.EndDate.Value)
        {
            return Result.Failure<bool>(AttendanceScheduleErrors.InvalidDateRange(command.StartDate.ToDateTime(TimeOnly.MinValue), command.EndDate.Value.ToDateTime(TimeOnly.MinValue)));
        }

        // Verify every referenced shift exists (only active days carry a shift)
        var referencedShiftIds = command.ScheduleDays
            .Where(d => d.ShiftId != Guid.Empty)
            .Select(d => d.ShiftId)
            .Distinct()
            .ToList();

        if (referencedShiftIds.Count > 0)
        {
            int existingShiftCount = await context.Shifts
                .AsNoTracking()
                .CountAsync(s => referencedShiftIds.Contains(s.Id), cancellationToken);

            if (existingShiftCount != referencedShiftIds.Count)
            {
                return Result.Failure<bool>(AttendanceScheduleErrors.ShiftNotFound());
            }
        }

        // Get Friday and Saturday dates in the date range and merge with user-provided excluded dates
        List<DateOnly> fridayAndSaturdayDates = GetFridayAndSaturdayDates(command.StartDate, command.EndDate);
        List<DateOnly> excludedDates = command.ExcludedDates ?? new List<DateOnly>();

        // Merge and remove duplicates using HashSet
        var excludedDatesSet = new HashSet<DateOnly>(excludedDates);
        foreach (DateOnly date in fridayAndSaturdayDates)
        {
            excludedDatesSet.Add(date);
        }

        var attendanceSchedule = new AttendanceSchedule
        {
            EmployeeId = command.EmployeeId,
            StartDate = command.StartDate,
            EndDate = command.EndDate,
            ScheduleType = command.ScheduleType,
            IsActive = command.IsActive,
            Notes = command.Notes,
            ExcludedDates = excludedDatesSet.ToList(),
            CreatedAt = dateTimeProvider.GetUtcNow()
        };

        foreach (CreateScheduleDayCommand scheduleDay in command.ScheduleDays)
        {
            var scheduleDayEntity = new ScheduleDay
            {
                AttendanceScheduleId = attendanceSchedule.Id,
                ScheduleDayDate = scheduleDay.ScheduleDayDate,
                ShiftId = scheduleDay.ShiftId,
                IsActive = scheduleDay.IsActive,
                Notes = scheduleDay.Notes,
                CreatedAt = dateTimeProvider.GetUtcNow()
            };

            attendanceSchedule.ScheduleDays.Add(scheduleDayEntity);
        }

        context.AttendanceSchedules.Add(attendanceSchedule);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }

    private static List<DateOnly> GetFridayAndSaturdayDates(DateOnly startDate, DateOnly? endDate)
    {
        var excludedDates = new List<DateOnly>();
        DateOnly currentDate = startDate;
        DateOnly end = endDate ?? startDate;

        while (currentDate <= end)
        {
            if (currentDate.DayOfWeek == System.DayOfWeek.Friday ||
                currentDate.DayOfWeek == System.DayOfWeek.Saturday)
            {
                excludedDates.Add(currentDate);
            }
            currentDate = currentDate.AddDays(1);
        }

        return excludedDates;
    }
}
