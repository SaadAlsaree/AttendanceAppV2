using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Models;
using Domain.Entities.Devices;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Dashboard.GetQuickStats;

internal sealed class GetQuickStatsQueryHandler(
    IApplicationDbContext context,

    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetQuickStatsQuery, ApiResponse<QuickStatsResponse>>
{
    public async Task<Result<ApiResponse<QuickStatsResponse>>> Handle(GetQuickStatsQuery query, CancellationToken cancellationToken)
    {
        try
        {
            // تحديد التاريخ مع التعامل الصحيح مع DateTime
            DateTime date = query.Date.HasValue
                ? dateTimeProvider.EnsureUtc(query.Date.Value)
                : dateTimeProvider.EnsureUtc(dateTimeProvider.GetCurrentLocalTime().Date);

            var response = new QuickStatsResponse { Date = date };

            // جلب عدد الموظفين - فلترة حسب الموظفين الذين لديهم سجلات حضور لهذه المنظمة
            response.TotalEmployees = await context.Employees
                .AsNoTracking()
                .Where(e => context.Attendances.Any(a => a.EmployeeId == e.Id && a.OrganizationId == query.OrganizationId))
                .CountAsync(cancellationToken);

            // check if user role not Admin then apply accessible unit ids filter



            // جلب إحصائيات الحضور اليوم
            List<Domain.Entities.Attendance.Attendance> todayAttendance = await context.Attendances
                .AsNoTracking()
                .Where(a => a.OrganizationId == query.OrganizationId && a.Date.Date == date.Date)
                .ToListAsync(cancellationToken);

            response.PresentToday = todayAttendance.Count(a => a.Status == AttendanceStatus.Present);
            response.AbsentToday = todayAttendance.Count(a => a.Status == AttendanceStatus.Absent);
            response.LateToday = todayAttendance.Count(a => a.Status == AttendanceStatus.Late);
            response.OnLeaveToday = todayAttendance.Count(a => a.Status == AttendanceStatus.Vacation);
            response.AttendanceRate = response.TotalEmployees > 0 ? (double)response.PresentToday / response.TotalEmployees * 100 : 0;



            // جلب إحصائيات الأجهزة
            List<Device> devices = await context.Devices
                .AsNoTracking()
                .Where(d => d.OrganizationId == query.OrganizationId)
                .ToListAsync(cancellationToken);

            response.OnlineDevices = devices.Count(d => d.IsActive);
            response.OfflineDevices = devices.Count(d => !d.IsActive);

            // جلب متوسط ساعات العمل
            var workingHours = todayAttendance.Where(a => a.WorkingMinutes.HasValue).ToList();
            response.AverageWorkingHours = workingHours.Any() ? workingHours.Average(a => a.WorkingMinutes!.Value) / 60.0 : 0;

            var overtimeHours = todayAttendance.Where(a => a.OvertimeMinutes.HasValue).ToList();
            response.AverageOvertimeHours = overtimeHours.Any() ? overtimeHours.Average(a => a.OvertimeMinutes!.Value) / 60.0 : 0;

            // جلب الموظفين في إجازة - فلترة حسب الموظفين الذين لديهم سجلات حضور لهذه المنظمة
            response.EmployeesOnLeave = await context.Leaves
                .AsNoTracking()
                .Where(l => context.Attendances.Any(a => a.EmployeeId == l.EmployeeId && a.OrganizationId == query.OrganizationId) &&
                           l.Status == LeaveStatus.Approved &&
                           l.StartDate <= date &&
                           l.EndDate >= date)
                .CountAsync(cancellationToken);



            // حساب التنبيهات النشطة
            int alerts = 0;
            if (response.OfflineDevices > 0)
            {
                alerts++;
            }
            if (response.PendingApprovals > 0)
            {
                alerts++;
            }
            if (response.AttendanceRate < 80)
            {
                alerts++; // انخفاض معدل الحضور
            }
            response.ActiveAlerts = alerts;

            return ApiResponse<QuickStatsResponse>.Success(response);
        }
        catch (Exception ex)
        {
            return ApiResponse<QuickStatsResponse>.Error($"Failed to retrieve quick statistics: {ex.Message}");
        }
    }
}
