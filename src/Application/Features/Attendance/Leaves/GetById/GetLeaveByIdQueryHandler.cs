using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Models;
using Domain.Entities.Attendance;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Attendance.Leaves.GetById;

internal sealed class GetLeaveByIdQueryHandler(
    IApplicationDbContext context,
    IHasPermission hasPermission,
    IUserContext userContext)
    : IQueryHandler<GetLeaveByIdQuery, LeaveResponse>
{
    public async Task<Result<LeaveResponse>> Handle(GetLeaveByIdQuery query, CancellationToken cancellationToken)
    {
        Leave? leave = await context.Leaves
            .Include(l => l.Employee)
            .FirstOrDefaultAsync(l => l.Id == query.LeaveId && !l.IsDeleted, cancellationToken);

        if (leave is null)
        {
            return Result.Failure<LeaveResponse>(LeaveErrors.NotFound(query.LeaveId));
        }

        // check if user role not Admin then apply accessible unit ids filter

        UserInfoDto user = await userContext.GetUserAsync();
        if (user.Role != Role.Admin)
        {
            IEnumerable<Guid> accessibleUnitIds = await hasPermission.GetAccessibleUnitIdsAsync(cancellationToken);
            if (!accessibleUnitIds.Contains(leave.Employee.OrganizationalUnitId!.Value))
            {
                return Result.Failure<LeaveResponse>(LeaveErrors.NotFound(query.LeaveId));
            }
        }

        var response = new LeaveResponse
        {
            Id = leave.Id,
            EmployeeId = leave.EmployeeId,
            FullName = leave.Employee.FullName,
            //Code = leave.Employee.Code,
            LeaveType = leave.LeaveType,
            StartDate = leave.StartDate,
            EndDate = leave.EndDate,
            Reason = leave.Reason,
            Status = leave.Status,
            ApprovedBy = leave.ApprovedBy,
            ApprovedAt = leave.ApprovedAt,
            RejectionReason = leave.RejectionReason,
            CreatedAt = leave.CreatedAt,
            LastUpdatedAt = leave.LastUpdatedAt
        };

        return response;
    }
}
