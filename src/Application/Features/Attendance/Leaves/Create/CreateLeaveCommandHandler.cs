using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Attendance;
using Domain.Entities.Organizations;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Attendance.Leaves.Create;

internal sealed class CreateLeaveCommandHandler(
    IApplicationDbContext context,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<CreateLeaveCommand, Guid>
{




    public async Task<Result<Guid>> Handle(CreateLeaveCommand command, CancellationToken cancellationToken)
    {
        // Check if employee exists
        bool employeeExists = await context.Employees
            .AsNoTracking()
            .AnyAsync(e => e.Id == command.EmployeeId, cancellationToken);

        if (!employeeExists)
        {
            return Result.Failure<Guid>(EmployeeErrors.NotFound(command.EmployeeId));
        }

        // Check if manager exists (if provided)
        if (command.ManagerId.HasValue)
        {
            bool managerExists = await context.Employees
                .AsNoTracking()
                .AnyAsync(e => e.Id == command.ManagerId.Value, cancellationToken);

            if (!managerExists)
            {
                return Result.Failure<Guid>(EmployeeErrors.NotFound(command.ManagerId.Value));
            }
        }

        // Convert dates to UTC for database storage
        DateTime startDateUtc = dateTimeProvider.EnsureUtc(command.StartDate);
        DateTime endDateUtc = dateTimeProvider.EnsureUtc(command.EndDate);

        // Validate date range
        if (startDateUtc >= endDateUtc)
        {
            return Result.Failure<Guid>(LeaveErrors.InvalidDateRange(command.StartDate, command.EndDate));
        }

        // Check if start date is not in the past
        if (startDateUtc.Date < dateTimeProvider.GetUtcNow().Date)
        {
            return Result.Failure<Guid>(LeaveErrors.StartDateInPast(command.StartDate));
        }

        // Check for overlapping leave requests
        bool overlappingLeave = await context.Leaves
            .AsNoTracking()
            .AnyAsync(l =>
                l.EmployeeId == command.EmployeeId &&
                l.Status != LeaveStatus.Rejected &&
                l.Status != LeaveStatus.Cancelled &&
                (l.StartDate <= startDateUtc && l.EndDate >= startDateUtc ||
                 l.StartDate <= endDateUtc && l.EndDate >= endDateUtc ||
                 l.StartDate >= startDateUtc && l.EndDate <= endDateUtc),
                cancellationToken);

        if (overlappingLeave)
        {
            return Result.Failure<Guid>(LeaveErrors.OverlappingLeave(command.EmployeeId, command.StartDate, command.EndDate));
        }

        var leave = new Leave
        {
            EmployeeId = command.EmployeeId,
            LeaveType = command.LeaveType,
            StartDate = startDateUtc,
            EndDate = endDateUtc,
            Reason = command.Reason,
            Status = LeaveStatus.Pending,
            CreatedAt = dateTimeProvider.GetUtcNow(),
            CreatedBy = userContext.UserId
        };

        context.Leaves.Add(leave);

        await context.SaveChangesAsync(cancellationToken);

        return leave.Id;
    }
}
