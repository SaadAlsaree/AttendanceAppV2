using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Attendance;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Attendance.Leaves.Reject;

internal sealed class RejectLeaveCommandHandler(
    IApplicationDbContext context)
    : ICommandHandler<RejectLeaveCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RejectLeaveCommand command, CancellationToken cancellationToken)
    {
        Leave? leave = await context.Leaves.FirstOrDefaultAsync(l => l.Id == command.LeaveId, cancellationToken);
        if (leave is null)
        {
            return Result.Failure<Guid>(LeaveErrors.NotFound(command.LeaveId));
        }

        if (leave.Status != LeaveStatus.Pending)
        {
            return Result.Failure<Guid>(LeaveErrors.LeaveAlreadyRejected(command.LeaveId));
        }

        if (string.IsNullOrWhiteSpace(command.RejectionReason))
        {
            return Result.Failure<Guid>(LeaveErrors.RejectionReasonRequired());
        }

        // TODO: Check rejector permissions if needed

        leave.Status = LeaveStatus.Rejected;
        leave.RejectionReason = command.RejectionReason;
        // Optionally store RejectionNotes if you add a property

        await context.SaveChangesAsync(cancellationToken);
        return leave.Id;
    }
}
