using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Authentication;
using Domain.Entities.Attendance;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using Application.Models;
using Domain.Users;

namespace Application.Attendance.Leaves.Approve;

internal sealed class ApproveLeaveCommandHandler(
    IApplicationDbContext context,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<ApproveLeaveCommand, Guid>
{
    public async Task<Result<Guid>> Handle(ApproveLeaveCommand command, CancellationToken cancellationToken)
    {
        Leave? leave = await context.Leaves.FirstOrDefaultAsync(l => l.Id == command.LeaveId, cancellationToken);
        if (leave is null)
        {
            return Result.Failure<Guid>(LeaveErrors.NotFound(command.LeaveId));
        }

        if (leave.Status != LeaveStatus.Pending)
        {
            return Result.Failure<Guid>(LeaveErrors.LeaveAlreadyApproved(command.LeaveId));
        }

        UserInfoDto user = await userContext.GetUserAsync();

        if (user.Role != Role.Manager)
        {
            return Result.Failure<Guid>(UserErrors.Unauthorized());
        }

        leave.Status = LeaveStatus.Approved;
        leave.ApprovedBy = command.ApprovedBy;
        leave.ApprovedAt = dateTimeProvider.GetUtcNow();
        // Optionally store ApprovalNotes if you add a property

        await context.SaveChangesAsync(cancellationToken);
        return leave.Id;
    }
}
