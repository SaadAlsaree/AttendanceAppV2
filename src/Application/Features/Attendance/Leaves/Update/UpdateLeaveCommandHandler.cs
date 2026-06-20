using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Models;
using Domain.Entities.Attendance;
using Domain.Entities.Organizations;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Attendance.Leaves.Update;

internal sealed class UpdateLeaveCommandHandler(
    IApplicationDbContext context,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<UpdateLeaveCommand, Guid>
{
    public async Task<Result<Guid>> Handle(UpdateLeaveCommand command, CancellationToken cancellationToken)
    {
        Leave? leave = await context.Leaves.FirstOrDefaultAsync(l => l.Id == command.LeaveId, cancellationToken);
        if (leave is null)
        {
            return Result.Failure<Guid>(LeaveErrors.NotFound(command.LeaveId));
        }

        // Object-level authorization (prevents IDOR): privileged roles may edit any
        // leave; everyone else may only edit leaves belonging to their own employee
        // record (Employee.UserId == caller). Done before any business rule below.
        UserInfoDto user = await userContext.GetUserAsync();
        bool isPrivileged = user.Role is Role.Admin or Role.SuperAdmin;
        if (!isPrivileged)
        {
            Employee? owner = await context.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.UserId == userContext.UserId, cancellationToken);
            if (owner is null || leave.EmployeeId != owner.Id)
            {
                return Result.Failure<Guid>(LeaveErrors.UnauthorizedUpdate(command.LeaveId));
            }
        }

        // Feature 08: editing a status (موقف) is open for 24h after it was recorded (Baghdad local time).
        // Anchored on the stored leave.CreatedAt, never on command dates (prevents bypass by editing the date).
        // Extension point (Feature 13): admins may bypass this window for long leaves.
        DateTime createdLocal = dateTimeProvider.ConvertToLocalTime(leave.CreatedAt);
        if (dateTimeProvider.Now > createdLocal.AddHours(24))
        {
            return Result.Failure<Guid>(LeaveErrors.EditWindowExpired(command.LeaveId));
        }

        // Apply the edited fields (the Leave entity has no Notes/EmergencyContact columns, so those are ignored).
        // Provided dates follow the same EnsureUtc convention as CreateLeaveCommandHandler; kept dates are
        // only re-stamped as UTC (the StartDate/EndDate properties map to timestamptz, which rejects
        // Kind=Unspecified values loaded from the underlying date column) without shifting the calendar day.
        leave.LeaveType = command.LeaveType ?? leave.LeaveType;
        leave.StartDate = command.StartDate.HasValue
            ? dateTimeProvider.EnsureUtc(command.StartDate.Value)
            : DateTime.SpecifyKind(leave.StartDate, DateTimeKind.Utc);
        leave.EndDate = command.EndDate.HasValue
            ? dateTimeProvider.EnsureUtc(command.EndDate.Value)
            : DateTime.SpecifyKind(leave.EndDate, DateTimeKind.Utc);
        leave.Reason = command.Reason ?? leave.Reason;

        // Check for overlapping leave requests against the new dates (excluding current leave)
        bool overlappingLeave = await context.Leaves
            .AsNoTracking()
            .AnyAsync(l =>
                l.Id != command.LeaveId &&
                l.EmployeeId == leave.EmployeeId &&
                l.Status != LeaveStatus.Rejected &&
                l.Status != LeaveStatus.Cancelled &&
                (l.StartDate <= leave.StartDate && l.EndDate >= leave.StartDate ||
                 l.StartDate <= leave.EndDate && l.EndDate >= leave.EndDate ||
                 l.StartDate >= leave.StartDate && l.EndDate <= leave.EndDate),
                cancellationToken);

        if (overlappingLeave)
        {
            return Result.Failure<Guid>(LeaveErrors.OverlappingLeave(leave.EmployeeId, leave.StartDate, leave.EndDate));
        }

        leave.LastUpdatedAt = dateTimeProvider.GetUtcNow();

        await context.SaveChangesAsync(cancellationToken);
        return leave.Id;
    }
}
