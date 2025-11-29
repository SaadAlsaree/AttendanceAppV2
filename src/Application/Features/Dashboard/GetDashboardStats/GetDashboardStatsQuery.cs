using Application.Abstractions.Messaging;

namespace Application.Features.Dashboard.GetDashboardStats;

public sealed record GetDashboardStatsQuery(
    Guid OrganizationId,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    Guid? DepartmentId = null,
    Guid? EmployeeId = null) : IQuery<DashboardStatsResponse>;
