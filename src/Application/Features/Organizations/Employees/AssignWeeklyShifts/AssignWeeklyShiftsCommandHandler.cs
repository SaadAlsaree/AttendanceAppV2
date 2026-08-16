using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Organizations;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using AttendanceEntity = Domain.Entities.Attendance.Attendance;

namespace Application.Features.Organizations.Employees.AssignWeeklyShifts;

internal sealed class AssignWeeklyShiftsCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext,
    IHasPermission hasPermission)
    : ICommandHandler<AssignWeeklyShiftsCommand>
{
    public async Task<Result> Handle(AssignWeeklyShiftsCommand command, CancellationToken cancellationToken)
    {
        // Check if employee exists
        Employee employee = await context.Employees
            .FirstOrDefaultAsync(e => e.Id == command.Id, cancellationToken);

        if (employee is null)
        {
            return Result.Failure(EmployeeErrors.NotFound(command.Id));
        }

        // Scoped roles (e.g. OrgSupervisor) may only manage employees in their own unit tree.
        if (!await hasPermission.CanManageEmployeeAsync(command.Id, cancellationToken))
        {
            return Result.Failure(EmployeeErrors.AccessDenied);
        }

        // Validate every referenced shift exists, is not deleted and is active
        var shiftIds = command.Days.Select(d => d.ShiftId).Distinct().ToList();

        if (shiftIds.Count > 0)
        {
            List<Shift> shifts = await context.Shifts
                .AsNoTracking()
                .Where(s => shiftIds.Contains(s.Id))
                .ToListAsync(cancellationToken);

            foreach (Guid shiftId in shiftIds)
            {
                Shift shift = shifts.FirstOrDefault(s => s.Id == shiftId && !s.IsDeleted);

                if (shift is null)
                {
                    return Result.Failure(ShiftErrors.NotFound(shiftId));
                }

                if (!shift.IsActive)
                {
                    return Result.Failure(ShiftErrors.InactiveShift(shiftId));
                }
            }
        }

        DateTime utcNow = dateTimeProvider.GetUtcNow();

        // Full replace: the submitted list becomes the employee's whole weekly pattern
        List<EmployeeWeeklyShift> existing = await context.EmployeeWeeklyShifts
            .Where(w => w.EmployeeId == command.Id)
            .ToListAsync(cancellationToken);

        context.EmployeeWeeklyShifts.RemoveRange(existing);

        foreach (WeeklyShiftDay day in command.Days)
        {
            context.EmployeeWeeklyShifts.Add(new EmployeeWeeklyShift
            {
                Id = Guid.NewGuid(),
                EmployeeId = command.Id,
                DayOfWeek = (System.DayOfWeek)day.DayOfWeek,
                ShiftId = day.ShiftId,
                CreatedAt = utcNow,
                CreatedBy = userContext.UserId
            });
        }

        employee.LastUpdatedAt = utcNow;
        employee.LastUpdatedBy = userContext.UserId;

        // Apply the new pattern to today's attendance record immediately, but only when the
        // record is still untouched (no check-in, still pending, not approved) and its shift
        // did not come from a schedule (schedules always win over the weekly pattern).
        var today = DateOnly.FromDateTime(dateTimeProvider.Now);
        var todayUtc = DateTime.SpecifyKind(today.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

        AttendanceEntity todayAttendance = await context.Attendances
            .FirstOrDefaultAsync(a =>
                a.EmployeeId == command.Id &&
                a.Date.Date == todayUtc &&
                a.CheckInTime == null &&
                a.Status == AttendanceStatus.Pending &&
                a.ApprovedBy == null &&
                a.AttendanceScheduleId == null,
                cancellationToken);

        if (todayAttendance is not null)
        {
            Guid? todayShiftId = command.Days
                .Where(d => d.DayOfWeek == (int)today.DayOfWeek)
                .Select(d => (Guid?)d.ShiftId)
                .FirstOrDefault();

            todayAttendance.ShiftId = todayShiftId;
            todayAttendance.LastUpdatedAt = utcNow;
            todayAttendance.LastUpdatedBy = userContext.UserId;
        }

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
