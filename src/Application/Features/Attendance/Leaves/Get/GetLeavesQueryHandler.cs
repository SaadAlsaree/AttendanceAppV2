using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Models;
using Domain.Entities.Attendance;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Attendance.Leaves.Get;

internal sealed class GetLeavesQueryHandler(
    IApplicationDbContext context,
    IHasPermission hasPermission,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetLeavesQuery, PaginatedResponse<LeaveResponse>>
{
    public async Task<Result<PaginatedResponse<LeaveResponse>>> Handle(GetLeavesQuery query, CancellationToken cancellationToken)
    {
        IQueryable<Leave> leavesQuery = context.Leaves
            .Include(l => l.Employee)
            .Where(l => !l.IsDeleted);

        // check if user role not Admin then apply accessible unit ids filter
        UserInfoDto user = await userContext.GetUserAsync();
        if (user.Role != Role.Admin)
        {
            IEnumerable<Guid> accessibleUnitIds = await hasPermission.GetAccessibleUnitIdsAsync(cancellationToken);
            leavesQuery = leavesQuery.Where(l => accessibleUnitIds.Contains(l.Employee.OrganizationalUnitId!.Value));
        }

        // Apply filters
        if (query.EmployeeId.HasValue)
        {
            leavesQuery = leavesQuery.Where(l => l.EmployeeId == query.EmployeeId.Value);
        }

        if (query.StartDate.HasValue)
        {
            DateTime startDateUtc = dateTimeProvider.EnsureUtc(query.StartDate.Value);
            leavesQuery = leavesQuery.Where(l => l.StartDate >= startDateUtc);
        }

        if (query.EndDate.HasValue)
        {
            DateTime endDateUtc = dateTimeProvider.EnsureUtc(query.EndDate.Value);
            leavesQuery = leavesQuery.Where(l => l.EndDate <= endDateUtc);
        }

        if (query.LeaveType.HasValue)
        {
            leavesQuery = leavesQuery.Where(l => l.LeaveType == query.LeaveType.Value);
        }

        if (query.Status.HasValue)
        {
            leavesQuery = leavesQuery.Where(l => l.Status == query.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            string searchTerm = query.SearchTerm;
            leavesQuery = leavesQuery.Where(l =>
                EF.Functions.Like(l.Employee.FullName, $"%{searchTerm}%") ||
                EF.Functions.Like(l.Employee.Code, $"%{searchTerm}%"));
        }

        // Apply sorting
        if (!string.IsNullOrWhiteSpace(query.SortBy))
        {
            string sortBy = query.SortBy.ToUpperInvariant();
            leavesQuery = sortBy switch
            {
                "STARTDATE" => query.SortOrder == SortOrder.Ascending
                    ? leavesQuery.OrderBy(l => l.StartDate)
                    : leavesQuery.OrderByDescending(l => l.StartDate),
                "ENDDATE" => query.SortOrder == SortOrder.Ascending
                    ? leavesQuery.OrderBy(l => l.EndDate)
                    : leavesQuery.OrderByDescending(l => l.EndDate),
                "STATUS" => query.SortOrder == SortOrder.Ascending
                    ? leavesQuery.OrderBy(l => l.Status)
                    : leavesQuery.OrderByDescending(l => l.Status),
                "LEAVETYPE" => query.SortOrder == SortOrder.Ascending
                    ? leavesQuery.OrderBy(l => l.LeaveType)
                    : leavesQuery.OrderByDescending(l => l.LeaveType),
                "CREATEDAT" => query.SortOrder == SortOrder.Ascending
                    ? leavesQuery.OrderBy(l => l.CreatedAt)
                    : leavesQuery.OrderByDescending(l => l.CreatedAt),
                _ => leavesQuery.OrderByDescending(l => l.CreatedAt)
            };
        }
        else
        {
            leavesQuery = leavesQuery.OrderByDescending(l => l.CreatedAt);
        }

        // Get total count
        int totalCount = await leavesQuery.CountAsync(cancellationToken);

        // Apply pagination
        int skip = (query.Page - 1) * query.PageSize;
        List<LeaveResponse> leaves = await leavesQuery
            .Skip(skip)
            .Take(query.PageSize)
            .Select(l => new LeaveResponse
            {
                Id = l.Id,
                EmployeeId = l.EmployeeId,
                FullName = l.Employee.FullName,
                //Code = l.Employee.Code,
                LeaveType = l.LeaveType,
                StartDate = l.StartDate,
                EndDate = l.EndDate,
                Reason = l.Reason,
                Status = l.Status,
                ApprovedBy = l.ApprovedBy,
                ApprovedAt = l.ApprovedAt,
                RejectionReason = l.RejectionReason,
                CreatedAt = l.CreatedAt,
                LastUpdatedAt = l.LastUpdatedAt
            })
            .ToListAsync(cancellationToken);

        return PaginatedResponse<LeaveResponse>.Create(
            leaves,
            totalCount,
            query.Page,
            query.PageSize);
    }
}
