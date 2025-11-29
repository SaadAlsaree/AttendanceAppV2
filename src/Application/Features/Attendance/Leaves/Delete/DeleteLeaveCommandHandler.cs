using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Attendance;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Attendance.Leaves.Delete;

internal sealed class DeleteLeaveCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<DeleteLeaveCommand, bool>
{
    public async Task<Result<bool>> Handle(DeleteLeaveCommand command, CancellationToken cancellationToken)
    {
        Leave? leave = await context.Leaves.FirstOrDefaultAsync(l => l.Id == command.LeaveId, cancellationToken);
        if (leave is null)
        {
            return Result.Failure<bool>(LeaveErrors.NotFound(command.LeaveId));
        }

        if (leave.Status == LeaveStatus.Approved)
        {
            return Result.Failure<bool>(LeaveErrors.CannotDeleteApprovedLeave(command.LeaveId));
        }

        if (leave.StartDate <= dateTimeProvider.GetUtcNow())
        {
            return Result.Failure<bool>(LeaveErrors.LeaveHasStarted(command.LeaveId));
        }

        // Soft delete
        leave.IsDeleted = true;
        leave.DeletedAt = dateTimeProvider.GetUtcNow();

        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
