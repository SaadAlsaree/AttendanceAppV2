using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Models;
using Domain.Entities.Attendance;
using Domain.Entities.Devices;
using Domain.Entities.Organizations;
using Domain.Enums;
using Application.Extensions;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Dashboard.GetDashboardStats;

internal sealed class GetDashboardStatsQueryHandler(
    IApplicationDbContext context,
    IHasPermission hasPermission,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetDashboardStatsQuery, DashboardStatsResponse>
{
    public async Task<Result<DashboardStatsResponse>> Handle(GetDashboardStatsQuery query, CancellationToken cancellationToken)
    {
        try
        {
            // تحديد نطاق التاريخ مع التعامل الصحيح مع DateTime
            DateTime currentDate = dateTimeProvider.GetUtcNow().Date;
            DateTime startDate = query.StartDate.HasValue
                ? dateTimeProvider.EnsureUtc(query.StartDate.Value)
                : currentDate.AddDays(-30);
            DateTime endDate = query.EndDate.HasValue
                ? dateTimeProvider.EnsureUtc(query.EndDate.Value)
                : currentDate;

            // التحقق من صحة نطاق التاريخ
            if (startDate >= endDate)
            {
                return Result.Failure<DashboardStatsResponse>(Error.Failure("Dashboard.Error", "Invalid date range"));
            }

            var response = new DashboardStatsResponse();

            // جلب إحصائيات الموظفين
            await PopulateEmployeeStats(response, query, cancellationToken);

            // جلب إحصائيات الحضور
            await PopulateAttendanceStats(response, query, startDate, endDate, cancellationToken);

            // جلب إحصائيات الأجهزة
            await PopulateDeviceStats(response, query, cancellationToken);

            // جلب إحصائيات الإجازات
            await PopulateLeaveStats(response, query, cancellationToken);

            // جلب مقاييس الأداء
            await PopulatePerformanceMetrics(response, query, startDate, endDate, cancellationToken);



            // جلب التنبيهات
            //await PopulateAlerts(response, query, cancellationToken);

            return Result.Success(response);
        }
        catch (Exception ex)
        {
            return Result.Failure<DashboardStatsResponse>(Error.Failure("Dashboard.Error", $"Failed to retrieve dashboard statistics: {ex.Message}"));
        }
    }

    private async Task PopulateEmployeeStats(DashboardStatsResponse response, GetDashboardStatsQuery query, CancellationToken cancellationToken)
    {
        // بناء الاستعلام الأساسي للموظفين مع تحسين الأداء
        IQueryable<Employee> employeeQuery = context.Employees
            .AsNoTracking()
            .Include(e => e.OrganizationalUnit)
            .Where(e => context.Attendances.Any(a => a.EmployeeId == e.Id && a.OrganizationId == query.OrganizationId))
            .AsSplitQuery();

        // check if user role not Admin then apply accessible unit ids filter
        UserInfoDto user = await userContext.GetUserAsync();
        if (user.Role != Role.Admin)
        {
            IEnumerable<Guid> accessibleUnitIds = await hasPermission.GetAccessibleUnitIdsAsync(cancellationToken);
            employeeQuery = employeeQuery.Where(e => accessibleUnitIds.Contains(e.OrganizationalUnitId!.Value));
        }

        // تطبيق الفلاتر
        if (query.DepartmentId.HasValue)
        {
            employeeQuery = employeeQuery.Where(e => e.OrganizationalUnitId == query.DepartmentId);
        }

        if (query.EmployeeId.HasValue)
        {
            employeeQuery = employeeQuery.Where(e => e.Id == query.EmployeeId);
        }

        // حساب الإحصائيات
        response.TotalEmployees = await employeeQuery.CountAsync(cancellationToken);
        response.ActiveEmployees = await employeeQuery.Where(e => e.CreatedAt <= dateTimeProvider.GetUtcNow().Date).CountAsync(cancellationToken);

        // جلب إحصائيات الحضور اليوم
        DateTime todayUtc = dateTimeProvider.GetUtcNow().Date;
        DateTime tomorrowUtc = todayUtc.AddDays(1);
        // جلب إحصائيات الحضور اليوم مع تطبيق الفلاتر
        IQueryable<Domain.Entities.Attendance.Attendance> todayAttendanceQuery = context.Attendances
            .AsNoTracking()
            .Include(a => a.Employee)
            .Where(a => a.OrganizationId == query.OrganizationId && a.Date >= todayUtc && a.Date < tomorrowUtc);

        if (query.DepartmentId.HasValue)
        {
            todayAttendanceQuery = todayAttendanceQuery.Where(a => a.Employee.OrganizationalUnitId == query.DepartmentId);
        }

        if (query.EmployeeId.HasValue)
        {
            todayAttendanceQuery = todayAttendanceQuery.Where(a => a.EmployeeId == query.EmployeeId);
        }

        List<Domain.Entities.Attendance.Attendance> todayAttendance = await todayAttendanceQuery.ToListAsync(cancellationToken);

        // اعتمد على اللقطة المخزنة (LateMinutes محسوبة مع فترة السماح) بدل إعادة الحساب من الجدول
        response.PresentToday = todayAttendance.Count(a => a.Status == AttendanceStatus.Present);
        response.LateToday = todayAttendance.Count(a => a.CheckInTime.HasValue && (a.LateMinutes ?? 0) > 0);
        response.OnLeaveToday = todayAttendance.Count(a => a.Status == AttendanceStatus.Vacation);
        response.RemoteWorkToday = todayAttendance.Count(a => a.Status == AttendanceStatus.Break);
    }

    private async Task PopulateAttendanceStats(DashboardStatsResponse response, GetDashboardStatsQuery query, DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        // بناء الاستعلام الأساسي للحضور مع تحسين الأداء
        IQueryable<Domain.Entities.Attendance.Attendance> attendanceQuery = context.Attendances
            .AsNoTracking()
            .Include(a => a.Employee)
            .ThenInclude(e => e.OrganizationalUnit)
            .Where(a => a.OrganizationId == query.OrganizationId &&
                       a.Date >= startDate &&
                       a.Date < endDate.AddDays(1))
            .AsSplitQuery();

        // تطبيق الفلاتر
        if (query.DepartmentId.HasValue)
        {
            attendanceQuery = attendanceQuery.Where(a => a.Employee.OrganizationalUnitId == query.DepartmentId);
        }

        if (query.EmployeeId.HasValue)
        {
            attendanceQuery = attendanceQuery.Where(a => a.EmployeeId == query.EmployeeId);
        }

        List<Domain.Entities.Attendance.Attendance> attendances = await attendanceQuery.ToListAsync(cancellationToken);

        // حساب إحصائيات الحضور
        response.AttendanceStats.TotalCheckIns = attendances.Count(a => a.CheckInTime.HasValue);
        response.AttendanceStats.TotalCheckOuts = attendances.Count(a => a.CheckOutTime.HasValue);

        var workingMinutesData = attendances.Where(a => a.WorkingMinutes.HasValue).ToList();
        response.AttendanceStats.AverageWorkingHours = workingMinutesData.Any()
            ? workingMinutesData.Average(a => a.WorkingMinutes!.Value) / 60.0
            : 0.0;

        var overtimeMinutesData = attendances.Where(a => a.OvertimeMinutes.HasValue).ToList();
        response.AttendanceStats.AverageOvertimeHours = overtimeMinutesData.Any()
            ? overtimeMinutesData.Average(a => a.OvertimeMinutes!.Value) / 60.0
            : 0.0;

        var lateMinutesData = attendances.Where(a => a.LateMinutes.HasValue).ToList();
        response.AttendanceStats.AverageLateMinutes = lateMinutesData.Any()
            ? lateMinutesData.Average(a => a.LateMinutes!.Value)
            : 0.0;

        var earlyLeaveMinutesData = attendances.Where(a => a.EarlyLeaveMinutes.HasValue).ToList();
        response.AttendanceStats.AverageEarlyLeaveMinutes = earlyLeaveMinutesData.Any()
            ? earlyLeaveMinutesData.Average(a => a.EarlyLeaveMinutes!.Value)
            : 0.0;

        // جلب إحصائيات السجلات
        //await PopulateLogStatistics(response, query, startDate, endDate, cancellationToken);

        // جلب إحصائيات الأقسام
        await PopulateDepartmentStats(response, query, cancellationToken);

        // جلب الاتجاهات
        await PopulateAttendanceTrends(response, query, startDate, endDate, cancellationToken);
    }

    //private async Task PopulateLogStatistics(DashboardStatsResponse response, GetDashboardStatsQuery query, DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    //{
    //    // بناء الاستعلام الأساسي للسجلات مع تحسين الأداء
    //    IQueryable<AttendanceLog> logsQuery = context.AttendanceLogs
    //        .AsNoTracking()
    //        .Include(l => l.Employee)
    //        .ThenInclude(e => e.OrganizationalUnit)
    //        .Where(l => l.OrganizationId == query.OrganizationId &&
    //                   l.Timestamp >= startDate &&
    //                   l.Timestamp < endDate.AddDays(1))
    //        .AsSplitQuery();

    //    // تطبيق الفلاتر
    //    if (query.DepartmentId.HasValue)
    //    {
    //        logsQuery = logsQuery.Where(l => l.Employee.OrganizationalUnitId == query.DepartmentId);
    //    }

    //    if (query.EmployeeId.HasValue)
    //    {
    //        logsQuery = logsQuery.Where(l => l.EmployeeId == query.EmployeeId);
    //    }

    //    List<AttendanceLog> logs = await logsQuery.ToListAsync(cancellationToken);

    //    response.AttendanceStats.PendingApprovals = logs.Count(l => l.Status == LogStatus.Pending);
    //    response.AttendanceStats.VerifiedLogs = logs.Count(l => l.IsVerified);
    //    response.AttendanceStats.RejectedLogs = logs.Count(l => l.Status == LogStatus.Rejected);
    //}

    private async Task PopulateDepartmentStats(DashboardStatsResponse response, GetDashboardStatsQuery query, CancellationToken cancellationToken)
    {
        // جلب جميع الأقسام التي لديها موظفين مع سجلات حضور
        List<OrganizationalUnit> departments = await context.OrganizationalUnits
            .AsNoTracking()
            .Include(ou => ou.Employees)
            .Where(ou => ou.Employees.Any(e => context.Attendances.Any(a => a.EmployeeId == e.Id && a.OrganizationId == query.OrganizationId)))
            .ToListAsync(cancellationToken);

        var departmentStats = new List<DepartmentAttendanceStats>();
        DateTime todayUtc = dateTimeProvider.GetUtcNow();

        foreach (OrganizationalUnit department in departments)
        {
            var employeeIds = department.Employees.Select(e => e.Id).ToList();

            // جلب بيانات الحضور للموظفين في هذا القسم
            List<Domain.Entities.Attendance.Attendance> attendances = await context.Attendances
                .AsNoTracking()
                .Where(a => employeeIds.Contains(a.EmployeeId) &&
                           a.OrganizationId == query.OrganizationId)
                .ToListAsync(cancellationToken);

            DateTime tomorrowUtc = todayUtc.AddDays(1);
            var todayAttendances = attendances.Where(a => a.Date >= todayUtc && a.Date < tomorrowUtc).ToList();
            var workingMinutesData = attendances.Where(a => a.WorkingMinutes.HasValue).ToList();

            var stats = new DepartmentAttendanceStats
            {
                DepartmentId = department.Id,
                DepartmentName = department.UnitName,
                TotalEmployees = department.Employees.Count,
                PresentCount = todayAttendances.Count(a => a.Status == AttendanceStatus.Present),
                AbsentCount = todayAttendances.Count(a => a.Status == AttendanceStatus.Absent),
                LateCount = todayAttendances.Count(a => a.Status == AttendanceStatus.Late),
                AttendanceRate = department.Employees.Count > 0
                    ? (double)todayAttendances.Count(a => a.Status == AttendanceStatus.Present) / department.Employees.Count * 100
                    : 0,
                AverageWorkingHours = workingMinutesData.Any()
                    ? workingMinutesData.Average(a => a.WorkingMinutes!.Value) / 60.0
                    : 0.0
            };

            departmentStats.Add(stats);
        }

        response.DepartmentStats = departmentStats;
    }

    private async Task PopulateAttendanceTrends(DashboardStatsResponse response, GetDashboardStatsQuery query, DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        // الاتجاهات اليومية للآخر 7 أيام
        List<DailyTrend> dailyTrends = await context.Attendances
            .AsNoTracking()
            .Where(a => a.OrganizationId == query.OrganizationId &&
                       a.Date >= startDate &&
                       a.Date < endDate.AddDays(1))
            .GroupBy(a => a.Date)
            .Select(g => new DailyTrend
            {
                Date = g.Key,
                PresentCount = g.Count(a => a.Status == AttendanceStatus.Present),
                AbsentCount = g.Count(a => a.Status == AttendanceStatus.Absent),
                LateCount = g.Count(a => a.Status == AttendanceStatus.Late),
                AverageWorkingHours = g.Any(a => a.WorkingMinutes.HasValue)
                    ? g.Where(a => a.WorkingMinutes.HasValue).Average(a => a.WorkingMinutes!.Value) / 60.0
                    : 0.0,
                AverageOvertimeHours = g.Any(a => a.OvertimeMinutes.HasValue)
                    ? g.Where(a => a.OvertimeMinutes.HasValue).Average(a => a.OvertimeMinutes!.Value) / 60.0
                    : 0.0
            })
            .OrderBy(t => t.Date)
            .Take(7)
            .ToListAsync(cancellationToken);

        response.AttendanceTrends.DailyTrends = dailyTrends;
    }

    private async Task PopulateDeviceStats(DashboardStatsResponse response, GetDashboardStatsQuery query, CancellationToken cancellationToken)
    {
        // بناء الاستعلام الأساسي للأجهزة مع تحسين الأداء
        IQueryable<Device> devicesQuery = context.Devices
            .AsNoTracking()
            .Where(d => d.OrganizationId == query.OrganizationId);

        List<Device> devices = await devicesQuery.ToListAsync(cancellationToken);

        // حساب إحصائيات الأجهزة
        response.DeviceStats.TotalDevices = devices.Count;
        response.DeviceStats.OnlineDevices = devices.Count(d => d.IsActive);
        response.DeviceStats.OfflineDevices = devices.Count(d => !d.IsActive);
        response.DeviceStats.UptimePercentage = devices.Count > 0 ? (double)response.DeviceStats.OnlineDevices / devices.Count * 100 : 0;

        // إنشاء قائمة حالة الأجهزة
        response.DeviceStatuses = devices.Select(d => new DeviceStatusInfo
        {
            DeviceId = d.Id,
            DeviceName = d.Username ?? string.Empty,
            Location = d.Location ?? string.Empty,

            IpAddress = d.IpAddress
        }).ToList();
    }

    private async Task PopulateLeaveStats(DashboardStatsResponse response, GetDashboardStatsQuery query, CancellationToken cancellationToken)
    {
        // بناء الاستعلام الأساسي للإجازات مع تحسين الأداء
        IQueryable<Leave> leavesQuery = context.Leaves
            .AsNoTracking()
            .Include(l => l.Employee)
            .ThenInclude(e => e.OrganizationalUnit)
            .Where(l => context.Attendances.Any(a => a.EmployeeId == l.EmployeeId && a.OrganizationId == query.OrganizationId))
            .AsSplitQuery();

        // تطبيق الفلاتر
        if (query.DepartmentId.HasValue)
        {
            leavesQuery = leavesQuery.Where(l => l.Employee.OrganizationalUnitId == query.DepartmentId);
        }

        if (query.EmployeeId.HasValue)
        {
            leavesQuery = leavesQuery.Where(l => l.EmployeeId == query.EmployeeId);
        }

        List<Leave> leaves = await leavesQuery.ToListAsync(cancellationToken);

        // حساب إحصائيات الإجازات
        response.LeaveStats.TotalLeaveRequests = leaves.Count;
        response.LeaveStats.PendingApprovals = leaves.Count(l => l.Status == LeaveStatus.Pending);
        response.LeaveStats.ApprovedLeaves = leaves.Count(l => l.Status == LeaveStatus.Approved);
        response.LeaveStats.RejectedLeaves = leaves.Count(l => l.Status == LeaveStatus.Rejected);
        DateTime currentDate = dateTimeProvider.GetUtcNow().Date;
        response.LeaveStats.EmployeesOnLeave = leaves.Count(l => l.Status == LeaveStatus.Approved &&
                                                                  l.StartDate <= currentDate &&
                                                                  l.EndDate >= currentDate);

        var approvedLeaves = leaves.Where(l => l.Status == LeaveStatus.Approved).ToList();
        response.LeaveStats.AverageLeaveDays = approvedLeaves.Any()
            ? approvedLeaves.Average(l => (l.EndDate - l.StartDate).Days + 1)
            : 0;

        // إحصائيات أنواع الإجازات
        List<LeaveTypeStats> leaveTypeStats = await leavesQuery
            .GroupBy(l => l.LeaveType)
            .Select(g => new LeaveTypeStats
            {
                LeaveType = g.Key,
                LeaveTypeName = g.Key.ToString(),
                RequestCount = g.Count(),
                ApprovedCount = g.Count(l => l.Status == LeaveStatus.Approved),
                RejectedCount = g.Count(l => l.Status == LeaveStatus.Rejected),
                ApprovalRate = g.Any() ? (double)g.Count(l => l.Status == LeaveStatus.Approved) / g.Count() * 100 : 0
            })
            .ToListAsync(cancellationToken);

        response.LeaveTypeStats = leaveTypeStats;
    }

    private async Task PopulatePerformanceMetrics(DashboardStatsResponse response, GetDashboardStatsQuery query, DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        // بناء الاستعلام الأساسي للحضور مع تحسين الأداء
        IQueryable<Domain.Entities.Attendance.Attendance> attendanceQuery = context.Attendances
            .AsNoTracking()
            .Include(a => a.Employee)
            .ThenInclude(e => e.OrganizationalUnit)
            .Where(a => a.OrganizationId == query.OrganizationId &&
                       a.Date >= startDate &&
                       a.Date < endDate.AddDays(1))
            .AsSplitQuery();

        // تطبيق الفلاتر
        if (query.DepartmentId.HasValue)
        {
            attendanceQuery = attendanceQuery.Where(a => a.Employee.OrganizationalUnitId == query.DepartmentId);
        }

        if (query.EmployeeId.HasValue)
        {
            attendanceQuery = attendanceQuery.Where(a => a.EmployeeId == query.EmployeeId);
        }

        List<Domain.Entities.Attendance.Attendance> attendances = await attendanceQuery.ToListAsync(cancellationToken);

        // حساب مقاييس الأداء
        int totalDays = attendances.Count;
        int presentDays = attendances.Count(a => a.Status == AttendanceStatus.Present);
        int onTimeDays = attendances.Count(a => a.LateMinutes == 0 || a.LateMinutes == null);

        response.PerformanceMetrics.OverallAttendanceRate = totalDays > 0 ? (double)presentDays / totalDays * 100 : 0;
        response.PerformanceMetrics.PunctualityRate = presentDays > 0 ? (double)onTimeDays / presentDays * 100 : 0;
        response.PerformanceMetrics.ProductivityScore = 85.0; // Placeholder - would be calculated based on business logic
        response.PerformanceMetrics.EmployeeSatisfactionScore = 78.0; // Placeholder - would come from surveys
        response.PerformanceMetrics.SystemUptime = response.DeviceStats.UptimePercentage;
        response.PerformanceMetrics.DataAccuracyRate = 98.5; // Placeholder - would be calculated based on data validation

        // جلب أفضل الأداء
        await PopulateTopPerformers(response, query, startDate, endDate, cancellationToken);
    }

    private async Task PopulateTopPerformers(DashboardStatsResponse response, GetDashboardStatsQuery query, DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        // بناء الاستعلام الأساسي للموظفين مع تحسين الأداء
        IQueryable<Employee> employeesQuery = context.Employees
            .AsNoTracking()
            .Include(e => e.OrganizationalUnit)
            .Where(e => context.Attendances.Any(a => a.EmployeeId == e.Id && a.OrganizationId == query.OrganizationId))
            .AsSplitQuery();

        // تطبيق الفلاتر
        if (query.DepartmentId.HasValue)
        {
            employeesQuery = employeesQuery.Where(e => e.OrganizationalUnitId == query.DepartmentId);
        }

        if (query.EmployeeId.HasValue)
        {
            employeesQuery = employeesQuery.Where(e => e.Id == query.EmployeeId);
        }

        List<EmployeePerformance> topPerformers = await employeesQuery
            .Select(e => new EmployeePerformance
            {
                EmployeeId = e.Id,
                FullName = e.FullName,
                //Code = e.Code,
                Department = e.OrganizationalUnit != null ? e.OrganizationalUnit.UnitName : string.Empty,
                AttendanceRate = 0, // Will be calculated separately
                PunctualityRate = 0, // Will be calculated separately
                AverageWorkingHours = 0, // Will be calculated separately
                OvertimeHours = 0, // Will be calculated separately
                LateCount = 0, // Will be calculated separately
                AbsentCount = 0 // Will be calculated separately
            })
            .ToListAsync(cancellationToken);

        // حساب مقاييس الأداء لكل موظف
        foreach (EmployeePerformance employee in topPerformers)
        {
            List<Domain.Entities.Attendance.Attendance> employeeAttendances = await context.Attendances
                .AsNoTracking()
                .Where(a => a.EmployeeId == employee.EmployeeId &&
                           a.OrganizationId == query.OrganizationId &&
                           a.Date >= startDate && a.Date < endDate.AddDays(1))
                .ToListAsync(cancellationToken);

            if (employeeAttendances.Any())
            {
                employee.AttendanceRate = (double)employeeAttendances.Count(a => a.Status == AttendanceStatus.Present) / employeeAttendances.Count * 100;

                var presentAttendances = employeeAttendances.Where(a => a.Status == AttendanceStatus.Present).ToList();
                employee.PunctualityRate = presentAttendances.Any()
                    ? (double)presentAttendances.Count(a => a.LateMinutes == 0 || a.LateMinutes == null) / presentAttendances.Count * 100
                    : 0;

                var workingMinutesData = employeeAttendances.Where(a => a.WorkingMinutes.HasValue).ToList();
                employee.AverageWorkingHours = workingMinutesData.Any()
                    ? workingMinutesData.Average(a => a.WorkingMinutes!.Value) / 60.0
                    : 0.0;

                var overtimeMinutesData = employeeAttendances.Where(a => a.OvertimeMinutes.HasValue).ToList();
                employee.OvertimeHours = overtimeMinutesData.Any()
                    ? overtimeMinutesData.Sum(a => a.OvertimeMinutes!.Value) / 60.0
                    : 0.0;

                employee.LateCount = employeeAttendances.Count(a => a.LateMinutes > 0);
                employee.AbsentCount = employeeAttendances.Count(a => a.Status == AttendanceStatus.Absent);
            }
        }

        // ترتيب حسب معدل الحضور وأخذ أفضل 10
        topPerformers = topPerformers.OrderByDescending(ep => ep.AttendanceRate).Take(10).ToList();

        response.TopPerformers = topPerformers;
    }



    //private async Task PopulateAlerts(DashboardStatsResponse response, GetDashboardStatsQuery query, CancellationToken cancellationToken)
    //{
    //    var alerts = new List<AlertItem>();

    //    // التحقق من الأجهزة غير المتصلة
    //    List<Device> offlineDevices = await context.Devices
    //        .AsNoTracking()
    //        .Where(d => d.OrganizationId == query.OrganizationId && !d.IsActive)
    //        .ToListAsync(cancellationToken);

    //    foreach (Device device in offlineDevices)
    //    {
    //        alerts.Add(new AlertItem
    //        {
    //            Id = Guid.NewGuid(),
    //            AlertType = "DeviceOffline",
    //            Title = "الجهاز غير متصل",
    //            Message = $"الجهاز {device.Username} في {device.Location} غير متصل",
    //            Severity = "تحذير",
    //            CreatedAt = dateTimeProvider.GetUtcNow(),
    //            IsResolved = false
    //        });
    //    }

    //    // التحقق من الموافقات المعلقة
    //    int pendingApprovals = await context.AttendanceLogs
    //        .AsNoTracking()
    //        .Where(l => l.OrganizationId == query.OrganizationId && l.AttendanceStatus == LogStatus.Pending)
    //        .CountAsync(cancellationToken);

    //    if (pendingApprovals > 0)
    //    {
    //        alerts.Add(new AlertItem
    //        {
    //            Id = Guid.NewGuid(),
    //            AlertType = "PendingApprovals",
    //            Title = "الموافقات المعلقة",
    //            Message = $"هناك {pendingApprovals} سجلات حضور معلقة",
    //            Severity = "معلومات",
    //            CreatedAt = dateTimeProvider.GetUtcNow(),
    //            IsResolved = false
    //        });
    //    }

    //    // التحقق من ارتفاع معدل الغياب
    //    int todayAbsent = response.AbsentToday;
    //    int totalEmployees = response.TotalEmployees;
    //    double absenteeismRate = totalEmployees > 0 ? (double)todayAbsent / totalEmployees * 100 : 0;

    //    if (absenteeismRate > 20) // أكثر من 20% غائب
    //    {
    //        alerts.Add(new AlertItem
    //        {
    //            Id = Guid.NewGuid(),
    //            AlertType = "HighAbsenteeism",
    //            Title = "ارتفاع معدل الغياب",
    //            Message = $"معدل الغياب هو {absenteeismRate:F1}% اليوم",
    //            Severity = "تحذير",
    //            CreatedAt = dateTimeProvider.GetUtcNow(),
    //            IsResolved = false
    //        });
    //    }

    //    response.Alerts = alerts;
    //}
}
