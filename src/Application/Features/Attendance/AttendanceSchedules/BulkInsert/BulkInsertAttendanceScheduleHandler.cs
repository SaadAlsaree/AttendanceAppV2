using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Extensions;
using Domain.Entities.Attendance;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Attendance.AttendanceSchedules.BulkInsert;

internal sealed class BulkInsertAttendanceScheduleHandler(IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<BulkInsertAttendanceScheduleCommand, bool>
{
    public async Task<Result<bool>> Handle(BulkInsertAttendanceScheduleCommand command, CancellationToken cancellationToken)
    {
        // Get all employee ids
        List<Guid> allEmployeeIds = await context.Employees
            .AsNoTracking()
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);

        if (!allEmployeeIds.Any())
        {
            return Result.Success(true);
        }

        // Find employees who already have an active schedule that overlaps the start date
        List<Guid> employeeIdsWithSchedule = await context.AttendanceSchedules
            .AsNoTracking()
            .Where(s => s.StartDate <= command.StartDate && (s.EndDate == null || s.EndDate >= command.StartDate) && s.IsActive)
            .Select(s => s.EmployeeId)
            .Distinct()
            .ToListAsync(cancellationToken);

        // Employees that need schedules created
        var targetEmployeeIds = allEmployeeIds.Except(employeeIdsWithSchedule).ToList();

        if (!targetEmployeeIds.Any())
        {
            return Result.Success(true);
        }

        DateTime now = dateTimeProvider.GetUtcNow();
        var schedules = new List<AttendanceSchedule>(capacity: targetEmployeeIds.Count);

        // Get Friday and Saturday dates in the date range
        List<DateOnly> fridayAndSaturdayDates = GetFridayAndSaturdayDates(command.StartDate, command.EndDate);

        foreach (Guid empId in targetEmployeeIds)
        {
            // CRITICAL: Generate a new Guid for the schedule BEFORE creating ScheduleDays
            var scheduleId = Guid.NewGuid();

            var schedule = new AttendanceSchedule
            {
                Id = scheduleId,  // Explicitly set the Id
                EmployeeId = empId,
                StartDate = command.StartDate,
                EndDate = command.EndDate,
                ScheduleType = ScheduleType.Regular,
                IsActive = true,
                ExcludedDates = fridayAndSaturdayDates,
                CreatedAt = now
            };

            // Create ScheduleDay for each date in the range [StartDate, EndDate]
            DateOnly currentDate = command.StartDate;
            DateOnly endDate = command.EndDate ?? command.StartDate;

            while (currentDate <= endDate)
            {
                var day = new ScheduleDay
                {
                    Id = Guid.NewGuid(),  // CRITICAL: Generate unique Id for each ScheduleDay
                    AttendanceScheduleId = scheduleId,  // Use the generated scheduleId
                    ScheduleDayDate = currentDate,
                    ShiftId = command.ShiftId,
                    IsActive = true,
                    CreatedAt = now
                };
                schedule.ScheduleDays.Add(day);
                currentDate = currentDate.AddDays(1);
            }

            schedules.Add(schedule);
        }

        // Batch insert to avoid huge memory/transaction for large number of employees
        // Try to cast to EF Core DbContext to adjust ChangeTracker for performance
        var efDbContext = context as Microsoft.EntityFrameworkCore.DbContext;
        if (efDbContext != null)
        {
            efDbContext.ChangeTracker.AutoDetectChangesEnabled = false;
        }

        const int batchSize = 500; // tune this value: 200-2000 depending on memory/DB
        for (int i = 0; i < schedules.Count; i += batchSize)
        {
            var batch = schedules.Skip(i).Take(batchSize).ToList();
            context.AttendanceSchedules.AddRange(batch);
            await context.SaveChangesAsync(cancellationToken);

            // Detach inserted entities to free memory
            if (efDbContext != null)
            {
                foreach (AttendanceSchedule ent in batch)
                {
                    Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry = efDbContext.Entry(ent);
                    entry.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                }
            }
        }

        if (efDbContext != null)
        {
            efDbContext.ChangeTracker.AutoDetectChangesEnabled = true;
        }

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
