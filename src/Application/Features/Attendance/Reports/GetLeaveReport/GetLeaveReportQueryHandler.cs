using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Models;
using Domain.Entities.Attendance;
using Domain.Enums;
using Domain.Reports;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Attendance.Reports.GetLeaveReport;

internal sealed class GetLeaveReportQueryHandler(
    IApplicationDbContext context,
    IHasPermission hasPermission,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetLeaveReportQuery, LeaveReportResponse>
{
    public async Task<Result<LeaveReportResponse>> Handle(GetLeaveReportQuery query, CancellationToken cancellationToken)
    {
        // Validate date range
        if (query.StartDate >= query.EndDate)
        {
            return Result.Failure<LeaveReportResponse>(ReportErrors.InvalidDateRange(query.StartDate, query.EndDate));
        }

        // Build base query for leave data
        IQueryable<Leave> leaveQuery = context.Leaves
            .AsNoTracking()
            .Include(l => l.Employee)
            .ThenInclude(e => e.OrganizationalUnit)
            .Where(l => l.StartDate >= query.StartDate && l.EndDate <= query.EndDate);

        // check if user role not Admin then apply accessible unit ids filter

        UserInfoDto user = await userContext.GetUserAsync();
        if (user.Role != Role.Admin)
        {
            IEnumerable<Guid> accessibleUnitIds = await hasPermission.GetAccessibleUnitIdsAsync(cancellationToken);
            leaveQuery = leaveQuery.Where(l => accessibleUnitIds.Contains(l.Employee.OrganizationalUnitId!.Value));
        }

        // Apply filters
        if (query.OrganizationId.HasValue)
        {
            // TODO: Implement organization filtering when organization relationship is available
            // leaveQuery = leaveQuery.Where(l => l.Employee.OrganizationalUnit.OrganizationId == query.OrganizationId.Value);
        }

        if (query.OrganizationalUnitId.HasValue)
        {
            leaveQuery = leaveQuery.Where(l => l.Employee.OrganizationalUnitId == query.OrganizationalUnitId.Value);
        }

        if (query.EmployeeId.HasValue)
        {
            leaveQuery = leaveQuery.Where(l => l.EmployeeId == query.EmployeeId.Value);
        }

        if (query.LeaveType.HasValue)
        {
            leaveQuery = leaveQuery.Where(l => l.LeaveType == query.LeaveType.Value);
        }

        if (query.LeaveStatus.HasValue)
        {
            leaveQuery = leaveQuery.Where(l => l.Status == query.LeaveStatus.Value);
        }

        List<Leave> leaves = await leaveQuery.ToListAsync(cancellationToken);

        if (!leaves.Any())
        {
            // Return empty response instead of error when no data is found
            var emptyResponse = new LeaveReportResponse
            {
                StartDate = query.StartDate,
                EndDate = query.EndDate,
                ReportType = query.ReportType,
                Statistics = new LeaveStatistics
                {
                    TotalLeaveRequests = 0,
                    ApprovedLeaves = 0,
                    PendingLeaves = 0,
                    RejectedLeaves = 0,
                    CancelledLeaves = 0,
                    TotalLeaveDays = 0,
                    AverageLeaveDuration = 0,
                    ApprovalRate = 0,
                    LeaveTypeDistribution = new Dictionary<LeaveType, int>()
                },
                EmployeeSummaries = new List<EmployeeLeaveSummary>(),
                DepartmentSummaries = new List<DepartmentLeaveSummary>(),
                GeneratedAt = dateTimeProvider.GetUtcNow()
            };

            return emptyResponse;
        }

        // Calculate statistics
        LeaveStatistics statistics = CalculateLeaveStatistics(leaves);

        // Generate employee summaries
        List<EmployeeLeaveSummary> employeeSummaries = GenerateEmployeeLeaveSummaries(leaves);

        // Generate department summaries
        List<DepartmentLeaveSummary> departmentSummaries = GenerateDepartmentLeaveSummaries(leaves);

        var response = new LeaveReportResponse
        {
            StartDate = query.StartDate,
            EndDate = query.EndDate,
            ReportType = query.ReportType,
            Statistics = statistics,
            EmployeeSummaries = employeeSummaries,
            DepartmentSummaries = departmentSummaries,
            GeneratedAt = dateTimeProvider.GetUtcNow()
        };

        return response;
    }

    private static LeaveStatistics CalculateLeaveStatistics(List<Leave> leaves)
    {
        int totalLeaveRequests = leaves.Count;
        int approvedLeaves = leaves.Count(l => l.Status == LeaveStatus.Approved);
        int pendingLeaves = leaves.Count(l => l.Status == LeaveStatus.Pending);
        int rejectedLeaves = leaves.Count(l => l.Status == LeaveStatus.Rejected);
        int cancelledLeaves = leaves.Count(l => l.Status == LeaveStatus.Cancelled);

        decimal totalLeaveDays = leaves.Sum(l => CalculateLeaveDays(l.StartDate, l.EndDate));
        decimal averageLeaveDuration = totalLeaveRequests > 0 ? totalLeaveDays / totalLeaveRequests : 0;
        decimal approvalRate = totalLeaveRequests > 0 ? (decimal)approvedLeaves / totalLeaveRequests * 100 : 0;

        // Calculate leave type distribution
        var leaveTypeDistribution = leaves
            .GroupBy(l => l.LeaveType)
            .ToDictionary(g => g.Key, g => g.Count());

        return new LeaveStatistics
        {
            TotalLeaveRequests = totalLeaveRequests,
            ApprovedLeaves = approvedLeaves,
            PendingLeaves = pendingLeaves,
            RejectedLeaves = rejectedLeaves,
            CancelledLeaves = cancelledLeaves,
            TotalLeaveDays = totalLeaveDays,
            AverageLeaveDuration = averageLeaveDuration,
            ApprovalRate = approvalRate,
            LeaveTypeDistribution = leaveTypeDistribution
        };
    }

    private static List<EmployeeLeaveSummary> GenerateEmployeeLeaveSummaries(List<Leave> leaves)
    {
        var summaries = leaves
            .GroupBy(l => l.EmployeeId)
            .Select(g => new EmployeeLeaveSummary
            {
                EmployeeId = g.Key,
                FullName = g.First().Employee.FullName,
                DepartmentName = g.First().Employee.OrganizationalUnit?.UnitName ?? "Unknown",
                TotalLeaveRequests = g.Count(),
                ApprovedLeaves = g.Count(l => l.Status == LeaveStatus.Approved),
                PendingLeaves = g.Count(l => l.Status == LeaveStatus.Pending),
                RejectedLeaves = g.Count(l => l.Status == LeaveStatus.Rejected),
                TotalLeaveDays = g.Sum(l => CalculateLeaveDays(l.StartDate, l.EndDate)),
                AverageLeaveDuration = g.Any() ? g.Sum(l => CalculateLeaveDays(l.StartDate, l.EndDate)) / g.Count() : 0,
                LeaveTypeCount = g.GroupBy(l => l.LeaveType).ToDictionary(gt => gt.Key, gt => gt.Count()),
                ApprovalRate = g.Any() ? (decimal)g.Count(l => l.Status == LeaveStatus.Approved) / g.Count() * 100 : 0
            })
            .ToList();

        return summaries;
    }

    private static List<DepartmentLeaveSummary> GenerateDepartmentLeaveSummaries(List<Leave> leaves)
    {
        var summaries = leaves
            .GroupBy(l => l.Employee.OrganizationalUnitId)
            .Select(g => new DepartmentLeaveSummary
            {
                DepartmentId = g.Key ?? Guid.Empty,
                DepartmentName = g.First().Employee.OrganizationalUnit?.UnitName ?? "Unknown",
                TotalEmployees = g.Select(l => l.EmployeeId).Distinct().Count(),
                EmployeesWithLeaves = g.Select(l => l.EmployeeId).Distinct().Count(),
                TotalLeaveRequests = g.Count(),
                ApprovedLeaves = g.Count(l => l.Status == LeaveStatus.Approved),
                PendingLeaves = g.Count(l => l.Status == LeaveStatus.Pending),
                RejectedLeaves = g.Count(l => l.Status == LeaveStatus.Rejected),
                TotalLeaveDays = g.Sum(l => CalculateLeaveDays(l.StartDate, l.EndDate)),
                AverageLeaveDuration = g.Any() ? g.Sum(l => CalculateLeaveDays(l.StartDate, l.EndDate)) / g.Count() : 0,
                ApprovalRate = g.Any() ? (decimal)g.Count(l => l.Status == LeaveStatus.Approved) / g.Count() * 100 : 0
            })
            .ToList();

        return summaries;
    }

    private static decimal CalculateLeaveDays(DateTime startDate, DateTime endDate)
    {
        return (endDate - startDate).Days + 1; // Inclusive of both start and end dates
    }
}
