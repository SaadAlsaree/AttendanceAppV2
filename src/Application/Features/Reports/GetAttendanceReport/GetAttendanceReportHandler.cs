using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Models;
using Domain.Entities.Organizations;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Reports.GetAttendanceReport;

internal sealed class GetAttendanceReportHandler(
    IApplicationDbContext context,
    IUserContext userContext,
    IHasPermission hasPermission,
    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetAttendanceReportQuery, ApiResponse<AttendanceReportVm>>
{
    public async Task<Result<ApiResponse<AttendanceReportVm>>> Handle(GetAttendanceReportQuery query, CancellationToken cancellationToken)
    {
        try
        {
            // التحقق من وجود الوحدة التنظيمية
            OrganizationalUnit? organizationalUnit = await context.OrganizationalUnits
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == query.OrganizationalUnitId, cancellationToken);

            if (organizationalUnit is null)
            {
                return Result.Failure<ApiResponse<AttendanceReportVm>>(
                    Error.NotFound("OrganizationalUnit.NotFound", "الجهة غير موجودة"));
            }

            // A scoped role may only report on a unit inside its own scope.
            // (Keyed by role name — SuperAdmin often has no unit and must stay global.)
            //
            // NOTE for SiteSupervisor: this guard alone is not sufficient. Further down, both
            // `query.IncludeSubUnits` and BuildSubUnitStatisticsAsync walk the requested unit's
            // children straight from the database, which would pull in units that are not members
            // of the site. That is why this endpoint is NOT granted to SiteSupervisor in
            // Web.Api/Endpoints/Reports/GetAttendanceReport.cs — the guard here is defence in depth
            // only. Fix those two descents before granting it.
            UserInfoDto currentUser = await userContext.GetUserAsync();
            if (currentUser.Role is Role.OrgSupervisor or Role.SiteSupervisor)
            {
                IEnumerable<Guid> accessibleUnitIds = await hasPermission.GetAccessibleUnitIdsAsync(cancellationToken);
                if (!accessibleUnitIds.Contains(query.OrganizationalUnitId))
                {
                    return Result.Failure<ApiResponse<AttendanceReportVm>>(
                        Error.Forbidden("Report.AccessDenied", "ليس لديك صلاحية لعرض تقرير جهة خارج نطاقك"));
                }
            }



            // تحديد نطاق التاريخ مع تحويل إلى UTC
            DateTime startDate = query.StartDate.HasValue
                ? dateTimeProvider.EnsureUtc(query.StartDate.Value.Date)
                : dateTimeProvider.EnsureUtc(dateTimeProvider.GetCurrentLocalTime().Date.AddDays(-30));
            DateTime endDate = query.EndDate.HasValue
                ? dateTimeProvider.EnsureUtc(query.EndDate.Value.Date.AddDays(1).AddTicks(-1)) // نهاية اليوم
                : dateTimeProvider.EnsureUtc(dateTimeProvider.GetCurrentLocalTime().Date);

            // جلب جميع الوحدات الفرعية إذا كان مطلوباً
            List<Guid> targetUnitIds = new() { query.OrganizationalUnitId };
            if (query.IncludeSubUnits)
            {
                List<Guid> subUnitIds = await GetSubUnitIdsAsync(query.OrganizationalUnitId, cancellationToken);
                targetUnitIds.AddRange(subUnitIds);
            }

            var report = new AttendanceReportVm
            {
                OrganizationalUnitId = organizationalUnit.Id,
                OrganizationalUnitName = organizationalUnit.UnitName,
                StartDate = startDate,
                EndDate = endDate,
                GeneratedAt = dateTimeProvider.GetCurrentLocalTime()
            };

            // بناء الإحصائيات العامة
            report.GeneralStats = await BuildGeneralStatisticsAsync(targetUnitIds, startDate, endDate, query.ShiftId, cancellationToken);

            // بناء إحصائيات الشفتات
            report.ShiftStats = await BuildShiftStatisticsAsync(targetUnitIds, startDate, endDate, query.ShiftId, cancellationToken);

            // بناء إحصائيات الوحدات الفرعية
            if (query.IncludeSubUnits)
            {
                report.SubUnitStats = await BuildSubUnitStatisticsAsync(query.OrganizationalUnitId, startDate, endDate, query.ShiftId, cancellationToken);
            }

            // بناء إحصائيات الإجازات
            report.LeaveStats = await BuildLeaveStatisticsAsync(targetUnitIds, startDate, endDate, cancellationToken);

            return Result.Success(new ApiResponse<AttendanceReportVm>
            {
                Data = report,
                Message = "تم إنشاء التقرير بنجاح",
                IsSuccess = true
            });
        }
        catch (Exception ex)
        {
            return Result.Failure<ApiResponse<AttendanceReportVm>>(
                Error.Failure("Report.GenerationFailed", $"فشل في إنشاء التقرير: {ex.Message}"));
        }
    }

    private async Task<List<Guid>> GetSubUnitIdsAsync(Guid parentUnitId, CancellationToken cancellationToken)
    {
        return await context.OrganizationalUnits
            .AsNoTracking()
            .Where(u => u.ParentUnitId == parentUnitId)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);
    }

    private async Task<GeneralStatistics> BuildGeneralStatisticsAsync(
        List<Guid> unitIds,
        DateTime startDate,
        DateTime endDate,
        Guid? shiftId,
        CancellationToken cancellationToken)
    {
        var stats = new GeneralStatistics();

        // جلب الموظفين في الوحدات المستهدفة
        IQueryable<Employee> employeesQuery = context.Employees
            .AsNoTracking()
            .Where(e => unitIds.Contains(e.OrganizationalUnitId ?? Guid.Empty));

        if (shiftId.HasValue)
        {
            // فلترة الموظفين حسب الشفت من خلال سجلات الحضور
            employeesQuery = employeesQuery.Where(e =>
                context.Attendances.Any(a => a.EmployeeId == e.Id && a.ShiftId == shiftId));
        }

        stats.TotalEmployees = await employeesQuery.CountAsync(cancellationToken);

        // جلب سجلات الحضور
        IQueryable<Domain.Entities.Attendance.Attendance> attendanceQuery = context.Attendances
            .AsNoTracking()
            .Where(a => unitIds.Contains(a.OrganizationId) &&
                       a.Date >= startDate &&
                       a.Date <= endDate &&
                       (a.CheckInTime != null || a.CheckOutTime != null));

        if (shiftId.HasValue)
        {
            attendanceQuery = attendanceQuery.Where(a => a.ShiftId == shiftId);
        }

        List<Domain.Entities.Attendance.Attendance> attendances = await attendanceQuery.ToListAsync(cancellationToken);

        // حساب الإحصائيات
        stats.TotalPresent = attendances.Count(a => a.Status == AttendanceStatus.Present);
        stats.TotalAbsent = attendances.Count(a => a.Status == AttendanceStatus.Absent);
        stats.TotalLate = attendances.Count(a => a.LateMinutes > 0);
        stats.TotalEarlyLeave = attendances.Count(a => a.EarlyLeaveMinutes > 0);
        stats.TotalOvertime = attendances.Count(a => a.OvertimeMinutes > 0);
        stats.TotalNoCheckIn = attendances.Count(a => a.CheckInTime == null);
        stats.TotalNoCheckOut = attendances.Count(a => a.CheckOutTime == null);
        stats.TotalOnLeave = attendances.Count(a => a.Status == AttendanceStatus.Vacation);

        // جلب عدد الشفتات
        stats.TotalShifts = await context.Shifts
            .AsNoTracking()
            .CountAsync(cancellationToken);

        // حساب النسب المئوية
        if (stats.TotalEmployees > 0)
        {
            stats.AttendanceRate = CalculatePercentage(stats.TotalPresent, stats.TotalEmployees);
            stats.AbsenceRate = CalculatePercentage(stats.TotalAbsent, stats.TotalEmployees);
            stats.LateRate = CalculatePercentage(stats.TotalLate, stats.TotalEmployees);
            stats.EarlyLeaveRate = CalculatePercentage(stats.TotalEarlyLeave, stats.TotalEmployees);
            stats.OvertimeRate = CalculatePercentage(stats.TotalOvertime, stats.TotalEmployees);
            stats.LeaveRate = CalculatePercentage(stats.TotalOnLeave, stats.TotalEmployees);
        }

        return stats;
    }

    private async Task<List<ShiftStatistics>> BuildShiftStatisticsAsync(
        List<Guid> unitIds,
        DateTime startDate,
        DateTime endDate,
        Guid? specificShiftId,
        CancellationToken cancellationToken)
    {
        var shiftStats = new List<ShiftStatistics>();

        // جلب الشفتات
        IQueryable<Shift> shiftsQuery = context.Shifts.AsNoTracking();

        if (specificShiftId.HasValue)
        {
            shiftsQuery = shiftsQuery.Where(s => s.Id == specificShiftId);
        }

        List<Shift> shifts = await shiftsQuery.ToListAsync(cancellationToken);

        foreach (Shift shift in shifts)
        {
            var shiftStat = new ShiftStatistics
            {
                ShiftId = shift.Id,
                ShiftName = shift.Name,
                StartTime = shift.StartTime,
                EndTime = shift.EndTime
            };

            // جلب الموظفين في هذا الشفت
            shiftStat.TotalEmployees = await context.Employees
                .AsNoTracking()
                .Where(e => unitIds.Contains(e.OrganizationalUnitId ?? Guid.Empty) &&
                           context.Attendances.Any(a => a.EmployeeId == e.Id && a.ShiftId == shift.Id))
                .CountAsync(cancellationToken);

            // جلب سجلات الحضور لهذا الشفت
            List<Domain.Entities.Attendance.Attendance> shiftAttendances = await context.Attendances
                .AsNoTracking()
                .Where(a => a.ShiftId == shift.Id &&
                           unitIds.Contains(a.OrganizationId) &&
                           a.Date >= startDate &&
                           a.Date <= endDate &&
                           (a.CheckInTime != null || a.CheckOutTime != null))
                .ToListAsync(cancellationToken);

            // حساب الإحصائيات
            shiftStat.PresentCount = shiftAttendances.Count(a => a.Status == AttendanceStatus.Present);
            shiftStat.AbsentCount = shiftAttendances.Count(a => a.Status == AttendanceStatus.Absent);
            shiftStat.LateCount = shiftAttendances.Count(a => a.LateMinutes > 0);
            shiftStat.EarlyLeaveCount = shiftAttendances.Count(a => a.EarlyLeaveMinutes > 0);
            shiftStat.OvertimeCount = shiftAttendances.Count(a => a.OvertimeMinutes > 0);
            shiftStat.NoCheckInCount = shiftAttendances.Count(a => a.CheckInTime == null);
            shiftStat.NoCheckOutCount = shiftAttendances.Count(a => a.CheckOutTime == null);
            shiftStat.OnLeaveCount = shiftAttendances.Count(a => a.Status == AttendanceStatus.Vacation);

            // حساب النسب المئوية
            if (shiftStat.TotalEmployees > 0)
            {
                shiftStat.AttendanceRate = CalculatePercentage(shiftStat.PresentCount, shiftStat.TotalEmployees);
                shiftStat.AbsenceRate = CalculatePercentage(shiftStat.AbsentCount, shiftStat.TotalEmployees);
                shiftStat.LateRate = CalculatePercentage(shiftStat.LateCount, shiftStat.TotalEmployees);
                shiftStat.EarlyLeaveRate = CalculatePercentage(shiftStat.EarlyLeaveCount, shiftStat.TotalEmployees);
                shiftStat.OvertimeRate = CalculatePercentage(shiftStat.OvertimeCount, shiftStat.TotalEmployees);
                shiftStat.LeaveRate = CalculatePercentage(shiftStat.OnLeaveCount, shiftStat.TotalEmployees);
            }

            shiftStats.Add(shiftStat);
        }

        return shiftStats;
    }

    private async Task<List<SubUnitStatistics>> BuildSubUnitStatisticsAsync(
        Guid parentUnitId,
        DateTime startDate,
        DateTime endDate,
        Guid? shiftId,
        CancellationToken cancellationToken)
    {
        var subUnitStats = new List<SubUnitStatistics>();

        // جلب الوحدات الفرعية
        List<OrganizationalUnit> subUnits = await context.OrganizationalUnits
            .AsNoTracking()
            .Where(u => u.ParentUnitId == parentUnitId)
            .ToListAsync(cancellationToken);

        foreach (OrganizationalUnit subUnit in subUnits)
        {
            var subUnitStat = new SubUnitStatistics
            {
                UnitId = subUnit.Id,
                UnitName = subUnit.UnitName,
                UnitCode = subUnit.UnitCode
            };

            // جلب الموظفين في الوحدة الفرعية
            IQueryable<Employee> employeesQuery = context.Employees
                .AsNoTracking()
                .Where(e => e.OrganizationalUnitId == subUnit.Id);

            if (shiftId.HasValue)
            {
                employeesQuery = employeesQuery.Where(e =>
                    context.Attendances.Any(a => a.EmployeeId == e.Id && a.ShiftId == shiftId));
            }

            subUnitStat.TotalEmployees = await employeesQuery.CountAsync(cancellationToken);

            // جلب عدد الشفتات
            subUnitStat.TotalShifts = await context.Shifts
                .AsNoTracking()
                .CountAsync(cancellationToken);

            // جلب سجلات الحضور
            IQueryable<Domain.Entities.Attendance.Attendance> attendanceQuery = context.Attendances
                .AsNoTracking()
                .Where(a => a.OrganizationId == subUnit.Id &&
                           a.Date >= startDate &&
                           a.Date <= endDate &&
                           (a.CheckInTime != null || a.CheckOutTime != null));

            if (shiftId.HasValue)
            {
                attendanceQuery = attendanceQuery.Where(a => a.ShiftId == shiftId);
            }

            List<Domain.Entities.Attendance.Attendance> attendances = await attendanceQuery.ToListAsync(cancellationToken);

            // حساب الإحصائيات
            subUnitStat.PresentCount = attendances.Count(a => a.Status == AttendanceStatus.Present);
            subUnitStat.AbsentCount = attendances.Count(a => a.Status == AttendanceStatus.Absent);
            subUnitStat.LateCount = attendances.Count(a => a.LateMinutes > 0);
            subUnitStat.EarlyLeaveCount = attendances.Count(a => a.EarlyLeaveMinutes > 0);
            subUnitStat.OvertimeCount = attendances.Count(a => a.OvertimeMinutes > 0);
            subUnitStat.NoCheckInCount = attendances.Count(a => a.CheckInTime == null);
            subUnitStat.NoCheckOutCount = attendances.Count(a => a.CheckOutTime == null);
            subUnitStat.OnLeaveCount = attendances.Count(a => a.Status == AttendanceStatus.Vacation);

            // حساب النسب المئوية
            if (subUnitStat.TotalEmployees > 0)
            {
                subUnitStat.AttendanceRate = CalculatePercentage(subUnitStat.PresentCount, subUnitStat.TotalEmployees);
                subUnitStat.AbsenceRate = CalculatePercentage(subUnitStat.AbsentCount, subUnitStat.TotalEmployees);
                subUnitStat.LateRate = CalculatePercentage(subUnitStat.LateCount, subUnitStat.TotalEmployees);
                subUnitStat.EarlyLeaveRate = CalculatePercentage(subUnitStat.EarlyLeaveCount, subUnitStat.TotalEmployees);
                subUnitStat.OvertimeRate = CalculatePercentage(subUnitStat.OvertimeCount, subUnitStat.TotalEmployees);
                subUnitStat.LeaveRate = CalculatePercentage(subUnitStat.OnLeaveCount, subUnitStat.TotalEmployees);
            }

            subUnitStats.Add(subUnitStat);
        }

        return subUnitStats;
    }

    private async Task<LeaveStatistics> BuildLeaveStatisticsAsync(
        List<Guid> unitIds,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        var leaveStats = new LeaveStatistics();

        // جلب الإجازات للموظفين في الوحدات المستهدفة
        List<Domain.Entities.Attendance.Leave> leaves = await context.Leaves
            .AsNoTracking()
            .Where(l => l.StartDate <= endDate &&
                       l.EndDate >= startDate &&
                       context.Employees.Any(e => e.Id == l.EmployeeId &&
                                                 unitIds.Contains(e.OrganizationalUnitId ?? Guid.Empty)))
            .ToListAsync(cancellationToken);

        // حساب الإحصائيات العامة
        leaveStats.TotalLeaves = leaves.Count;
        leaveStats.ApprovedLeaves = leaves.Count(l => l.Status == LeaveStatus.Approved);
        leaveStats.PendingLeaves = leaves.Count(l => l.Status == LeaveStatus.Pending);
        leaveStats.RejectedLeaves = leaves.Count(l => l.Status == LeaveStatus.Rejected);

        // حساب النسب المئوية
        if (leaveStats.TotalLeaves > 0)
        {
            leaveStats.ApprovalRate = CalculatePercentage(leaveStats.ApprovedLeaves, leaveStats.TotalLeaves);
            leaveStats.RejectionRate = CalculatePercentage(leaveStats.RejectedLeaves, leaveStats.TotalLeaves);
            leaveStats.PendingRate = CalculatePercentage(leaveStats.PendingLeaves, leaveStats.TotalLeaves);
        }

        // بناء إحصائيات أنواع الإجازات
        var leaveTypeGroups = leaves.GroupBy(l => l.LeaveType).ToList();
        foreach (IGrouping<LeaveType, Domain.Entities.Attendance.Leave> group in leaveTypeGroups)
        {
            var leaveTypeStat = new LeaveTypeStatistics
            {
                LeaveType = group.Key,
                LeaveTypeName = GetDisplayName(group.Key),
                Count = group.Count(),
                Percentage = CalculatePercentage(group.Count(), leaveStats.TotalLeaves)
            };
            leaveStats.LeaveTypeStats.Add(leaveTypeStat);
        }

        return leaveStats;
    }

    private static decimal CalculatePercentage(int count, int total)
    {
        if (total == 0)
        {
            return 0;
        }
        return Math.Round((decimal)count / total * 100, 2);
    }

    private static string GetDisplayName(LeaveType leaveType)
    {
        DisplayAttribute? displayAttribute = leaveType.GetType()
            .GetField(leaveType.ToString())
            ?.GetCustomAttribute<DisplayAttribute>();

        return displayAttribute?.Name ?? leaveType.ToString();
    }
}
