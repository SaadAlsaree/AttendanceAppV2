using Application.Abstractions.Messaging;
using Application.Features.Dashboard.GetDashboardStats;
using Application.Features.Dashboard.GetQuickStats;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Dashboard;

internal sealed class Dashboard : IEndpoint
{
    public sealed class Request
    {
        public Guid OrganizationId { get; set; }
        public DateTime? Date { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public Guid? DepartmentId { get; set; }
        public Guid? EmployeeId { get; set; }
    }

    public sealed class DashboardResponse
    {
        public QuickStatsResponse QuickStats { get; set; } = new();
        public DashboardStatsResponse DetailedStats { get; set; } = new();
        public DashboardNavigation Navigation { get; set; } = new();
    }

    public sealed class DashboardNavigation
    {
        public List<NavigationItem> QuickActions { get; set; } = [];
        public List<NavigationItem> Reports { get; set; } = [];
        public List<NavigationItem> Management { get; set; } = [];
    }

    public sealed class NavigationItem
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Route { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public bool RequiresPermission { get; set; }
        public string? Permission { get; set; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("dashboard", async (
            [AsParameters] Request request,
            IQueryHandler<GetQuickStatsQuery, ApiResponse<QuickStatsResponse>> quickStatsHandler,
            IQueryHandler<GetDashboardStatsQuery, DashboardStatsResponse> detailedStatsHandler,
            CancellationToken cancellationToken) =>
        {
            // Get quick stats
            var quickStatsQuery = new GetQuickStatsQuery(
                OrganizationId: request.OrganizationId,
                Date: request.Date
            );

            Result<ApiResponse<QuickStatsResponse>> quickStatsResult = await quickStatsHandler.Handle(quickStatsQuery, cancellationToken);

            // Get detailed stats
            var detailedStatsQuery = new GetDashboardStatsQuery(
                OrganizationId: request.OrganizationId,
                StartDate: request.StartDate,
                EndDate: request.EndDate,
                DepartmentId: request.DepartmentId,
                EmployeeId: request.EmployeeId
            );

            Result<DashboardStatsResponse> detailedStatsResult = await detailedStatsHandler.Handle(detailedStatsQuery, cancellationToken);

            // Combine results
            if (quickStatsResult.IsFailure)
            {
                return CustomResults.Problem(quickStatsResult);
            }

            if (detailedStatsResult.IsFailure)
            {
                return CustomResults.Problem(detailedStatsResult);
            }

            var response = new DashboardResponse
            {
                QuickStats = quickStatsResult.Value.Data ?? new(),
                DetailedStats = detailedStatsResult.Value,
                Navigation = CreateNavigation()
            };

            return Results.Ok(response);
        })
        .WithTags(Tags.Dashboard)
        .RequireAuthorization();
    }

    private static DashboardNavigation CreateNavigation()
    {
        return new DashboardNavigation
        {
            QuickActions =
            [
                new NavigationItem
                {
                    Title = "تسجيل الوصول",
                    Description = "تسجيل وصول الموظف",
                    Route = "/attendance/check-in",
                    Icon = "login",
                    RequiresPermission = true,
                    Permission = "attendance.create"
                },
                new NavigationItem
                {
                    Title = "تسجيل المغادرة",
                    Description = "تسجيل مغادرة الموظف",
                    Route = "/attendance/check-out",
                    Icon = "logout",
                    RequiresPermission = true,
                    Permission = "attendance.create"
                },
                new NavigationItem
                {
                    Title = "الإحصائيات السريعة",
                    Description = "عرض الإحصائيات السريعة للوحة التحكم",
                    Route = "/dashboard/quick-stats",
                    Icon = "trending_up",
                    RequiresPermission = false
                }
            ],
            Reports =
            [
                new NavigationItem
                {
                    Title = "تقرير الحضور",
                    Description = "عرض تقرير الحضور المفصل",
                    Route = "/reports/attendance",
                    Icon = "assessment",
                    RequiresPermission = true,
                    Permission = "reports.view"
                },
                new NavigationItem
                {
                    Title = "تقرير التأخير",
                    Description = "عرض تقرير التأخير",
                    Route = "/reports/late",
                    Icon = "schedule",
                    RequiresPermission = true,
                    Permission = "reports.view"
                },
                new NavigationItem
                {
                    Title = "تقرير الغياب",
                    Description = "عرض تقرير الغياب",
                    Route = "/reports/absences",
                    Icon = "event_busy",
                    RequiresPermission = true,
                    Permission = "reports.view"
                }
            ],
            Management =
            [
                new NavigationItem
                {
                    Title = "الموظفين",
                    Description = "إدارة الموظفين والملفات الشخصية",
                    Route = "/employees",
                    Icon = "people",
                    RequiresPermission = true,
                    Permission = "employees.view"
                },
                new NavigationItem
                {
                    Title = "الأجهزة",
                    Description = "إدارة الأجهزة للحضور والانصراف",
                    Route = "/devices",
                    Icon = "devices",
                    RequiresPermission = true,
                    Permission = "devices.view"
                },
                new NavigationItem
                {
                    Title = "المناوبات",
                    Description = "إدارة المناوبات",
                    Route = "/shifts",
                    Icon = "schedule",
                    RequiresPermission = true,
                    Permission = "shifts.view"
                },
                new NavigationItem
                {
                    Title = "الجهات",
                    Description = "إدارة الجهات",
                    Route = "/organizational-units",
                    Icon = "business",
                    RequiresPermission = true,
                    Permission = "organizational-units.view"
                }
            ]
        };
    }
}
