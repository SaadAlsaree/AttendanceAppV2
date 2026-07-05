using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Attendance;
using Domain.Entities.Organizations;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Attendance.Create;

internal sealed class CreateAttendanceCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider,
    IHasPermission hasPermission)
    : ICommandHandler<CreateAttendanceCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateAttendanceCommand command, CancellationToken cancellationToken)
    {
        // Check if employee exists and is active
        Employee? employee = await context.Employees
            .AsNoTracking()
            .Include(e => e.User)
            .SingleOrDefaultAsync(e => e.Id == command.EmployeeId, cancellationToken);

        if (employee is null)
        {
            return Result.Failure<Guid>(EmployeeErrors.NotFound(command.EmployeeId));
        }

        // Scoped roles (e.g. OrgSupervisor) may only manage employees in their own unit tree.
        if (!await hasPermission.CanManageEmployeeAsync(command.EmployeeId, cancellationToken))
        {
            return Result.Failure<Guid>(EmployeeErrors.AccessDenied);
        }

        // Check if employee's user is active
        if (employee.User is null || employee.User.Status != UserStatus.Active)
        {
            return Result.Failure<Guid>(EmployeeErrors.InactiveEmployee(command.EmployeeId));
        }

        // Check if attendance record already exists for the employee on the given date
        bool attendanceExists = await context.Attendances
            .AsNoTracking()
            .AnyAsync(a => a.EmployeeId == command.EmployeeId && a.Date.Date == command.Date.Date, cancellationToken);

        if (attendanceExists)
        {
            return Result.Failure<Guid>(AttendanceErrors.AlreadyCheckedIn(command.EmployeeId));
        }

        // Validate date is not in the future
        if (command.Date.Date > dateTimeProvider.Now.Date)
        {
            return Result.Failure<Guid>(AttendanceErrors.InvalidDateRange(command.Date, dateTimeProvider.Now));
        }

        // Validate organization exists
        OrganizationalUnit? organization = await context.OrganizationalUnits
            .AsNoTracking()
            .SingleOrDefaultAsync(o => o.Id == command.OrganizationId, cancellationToken);

        if (organization is null)
        {
            return Result.Failure<Guid>(OrganizationErrors.OrganizationalUnit.NotFound(command.OrganizationId));
        }

        // Validate shift exists if provided
        if (command.ShiftId.HasValue)
        {
            Shift? shift = await context.Shifts
                .AsNoTracking()
                .SingleOrDefaultAsync(s => s.Id == command.ShiftId, cancellationToken);

            if (shift is null)
            {
                return Result.Failure<Guid>(ShiftErrors.NotFound(command.ShiftId.Value));
            }
        }

        // Validate attendance schedule exists if provided
        if (command.AttendanceScheduleId.HasValue)
        {
            AttendanceSchedule? schedule = await context.AttendanceSchedules
                .AsNoTracking()
                .SingleOrDefaultAsync(s => s.Id == command.AttendanceScheduleId, cancellationToken);

            if (schedule is null)
            {
                return Result.Failure<Guid>(AttendanceScheduleErrors.NotFound(command.AttendanceScheduleId.Value));
            }
        }

        var attendance = new Domain.Entities.Attendance.Attendance
        {
            EmployeeId = command.EmployeeId,
            OrganizationId = command.OrganizationId,
            Date = command.Date.Date,
            ShiftId = command.ShiftId,
            AttendanceScheduleId = command.AttendanceScheduleId,
            Notes = command.Notes,
            Status = AttendanceStatus.Present,
            CreatedAt = dateTimeProvider.GetUtcNow()
        };

        attendance.Raise(new AttendanceCreatedDomainEvent(attendance.Id, attendance.EmployeeId, attendance.Date));

        context.Attendances.Add(attendance);

        await context.SaveChangesAsync(cancellationToken);

        return attendance.Id;
    }
}
