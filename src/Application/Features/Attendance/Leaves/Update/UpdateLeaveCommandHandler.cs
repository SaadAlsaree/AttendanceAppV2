using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Attendance;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Attendance.Leaves.Update;

internal sealed class UpdateLeaveCommandHandler(
    IApplicationDbContext context,
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


        // Check for overlapping leave requests (excluding current leave)
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
