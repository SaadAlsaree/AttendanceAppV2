using Application.Abstractions.Messaging;
using SharedKernel;

namespace Application.Features.Dashboard.GetQuickStats;

public sealed record GetQuickStatsQuery(
    Guid OrganizationId,
    DateTime? Date = null) : IQuery<ApiResponse<QuickStatsResponse>>;
