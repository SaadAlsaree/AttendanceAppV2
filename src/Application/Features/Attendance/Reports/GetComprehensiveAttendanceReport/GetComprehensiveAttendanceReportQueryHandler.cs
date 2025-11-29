using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Models;
using Domain.Entities.Attendance;
using Application.Extensions;
using Domain.Enums;
using Domain.Reports;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Attendance.Reports.GetComprehensiveAttendanceReport;

internal sealed class GetComprehensiveAttendanceReportQueryHandler(
    IApplicationDbContext context,
    IHasPermission hasPermission,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetComprehensiveAttendanceReportQuery, ComprehensiveAttendanceReportResponse>
{
    public async Task<Result<ComprehensiveAttendanceReportResponse>> Handle(GetComprehensiveAttendanceReportQuery query, CancellationToken cancellationToken)
    {
        // التحقق من صحة نطاق التاريخ
        if (query.StartDate >= query.EndDate)
        {
            return Result.Failure<ComprehensiveAttendanceReportResponse>(ReportErrors.InvalidDateRange(query.StartDate, query.EndDate));
        }

        // بناء الاستعلام الأساسي للحضور مع تحسين الأداء
        IQueryable<Domain.Entities.Attendance.Attendance> attendanceQuery = context.Attendances
            .AsNoTracking()
            .Include(a => a.Employee)
            .ThenInclude(e => e.OrganizationalUnit)
            .Include(a => a.Breaks)
            .Include(a => a.Shift)
            .Include(a => a.AttendanceSchedule)
            .Where(a => a.Date >= query.StartDate && a.Date <= query.EndDate)
            .AsSplitQuery(); // تقسيم الاستعلام لتحسين الأداء

        // check if user role not Admin then apply accessible unit ids filter

        UserInfoDto user = await userContext.GetUserAsync();
        if (user.Role != Role.Admin)
        {
            IEnumerable<Guid> accessibleUnitIds = await hasPermission.GetAccessibleUnitIdsAsync(cancellationToken);
            attendanceQuery = attendanceQuery.Where(a => accessibleUnitIds.Contains(a.Employee.OrganizationalUnitId!.Value));
        }

        // Apply filters
        if (query.OrganizationId.HasValue)
        {
            attendanceQuery = attendanceQuery.Where(a => a.OrganizationId == query.OrganizationId.Value);
        }

        if (query.OrganizationalUnitId.HasValue)
        {
            attendanceQuery = attendanceQuery.Where(a => a.Employee.OrganizationalUnitId == query.OrganizationalUnitId.Value);
        }

        if (query.EmployeeId.HasValue)
        {
            attendanceQuery = attendanceQuery.Where(a => a.EmployeeId == query.EmployeeId.Value);
        }

        if (query.ManagerId.HasValue)
        {
            attendanceQuery = attendanceQuery.Where(a => a.Employee.ManagerId == query.ManagerId.Value);
        }

        List<Domain.Entities.Attendance.Attendance> attendances = await attendanceQuery.ToListAsync(cancellationToken);

        // إذا لم توجد بيانات حضور، هذا طبيعي - قد يكون الموظفون لم يسجلوا حضور بعد

        // جلب الجداول الزمنية النشطة للموظفين
        List<AttendanceSchedule> activeSchedules = await GetActiveSchedules(query, cancellationToken);

        // التحقق من وجود جداول زمنية
        if (!activeSchedules.Any())
        {
            // إذا لم توجد جداول زمنية، نعيد تقرير فارغ بدلاً من خطأ
            var emptyResponse = new ComprehensiveAttendanceReportResponse
            {
                StartDate = query.StartDate,
                EndDate = query.EndDate,
                ReportType = query.ReportType,
                Statistics = new ComprehensiveAttendanceStatistics(),
                EmployeeSummaries = new List<ComprehensiveEmployeeSummary>(),
                DepartmentSummaries = new List<ComprehensiveDepartmentSummary>(),
                DailySummaries = new List<DailyAttendanceSummary>(),
                GeneratedAt = dateTimeProvider.GetUtcNow()
            };
            return Result.Success(emptyResponse);
        }

        // حساب الأيام المتوقعة
        Dictionary<Guid, Dictionary<DateTime, ExpectedWorkDay>> expectedWorkingDays = await CalculateExpectedWorkingDays(
            query.StartDate, query.EndDate, activeSchedules);

        // التحقق من وجود أيام متوقعة
        if (!expectedWorkingDays.Any())
        {
            // إذا لم توجد أيام متوقعة، نعيد تقرير فارغ بدلاً من خطأ
            // هذا قد يحدث إذا كانت الجداول الزمنية لا تحتوي على أيام عمل محددة
            var emptyResponse = new ComprehensiveAttendanceReportResponse
            {
                StartDate = query.StartDate,
                EndDate = query.EndDate,
                ReportType = query.ReportType,
                Statistics = new ComprehensiveAttendanceStatistics(),
                EmployeeSummaries = new List<ComprehensiveEmployeeSummary>(),
                DepartmentSummaries = new List<ComprehensiveDepartmentSummary>(),
                DailySummaries = new List<DailyAttendanceSummary>(),
                GeneratedAt = dateTimeProvider.GetUtcNow()
            };
            return Result.Success(emptyResponse);
        }

        // التحقق من وجود بيانات
        if (!attendances.Any())
        {
            // إذا لم توجد بيانات حضور، نعيد تقرير مع الأيام المتوقعة فقط
            var responseWithExpectedDays = new ComprehensiveAttendanceReportResponse
            {
                StartDate = query.StartDate,
                EndDate = query.EndDate,
                ReportType = query.ReportType,
                Statistics = CalculateComprehensiveStatistics(new List<Domain.Entities.Attendance.Attendance>(), expectedWorkingDays),
                EmployeeSummaries = GenerateComprehensiveEmployeeSummaries(new List<Domain.Entities.Attendance.Attendance>(), expectedWorkingDays),
                DepartmentSummaries = new List<ComprehensiveDepartmentSummary>(),
                DailySummaries = GenerateDailySummaries(new List<Domain.Entities.Attendance.Attendance>(), expectedWorkingDays),
                GeneratedAt = dateTimeProvider.GetUtcNow()
            };
            return Result.Success(responseWithExpectedDays);
        }

        // حساب الإحصائيات الشاملة
        ComprehensiveAttendanceStatistics statistics = CalculateComprehensiveStatistics(attendances, expectedWorkingDays);

        // إنشاء ملخصات الموظفين
        List<ComprehensiveEmployeeSummary> employeeSummaries = GenerateComprehensiveEmployeeSummaries(attendances, expectedWorkingDays);

        // إنشاء ملخصات الأقسام
        List<ComprehensiveDepartmentSummary> departmentSummaries = GenerateComprehensiveDepartmentSummaries(attendances);

        // إنشاء ملخصات يومية
        List<DailyAttendanceSummary> dailySummaries = GenerateDailySummaries(attendances, expectedWorkingDays);

        var response = new ComprehensiveAttendanceReportResponse
        {
            StartDate = query.StartDate,
            EndDate = query.EndDate,
            ReportType = query.ReportType,
            Statistics = statistics,
            EmployeeSummaries = employeeSummaries,
            DepartmentSummaries = departmentSummaries,
            DailySummaries = dailySummaries,
            GeneratedAt = dateTimeProvider.GetUtcNow()
        };

        return response;
    }

    private async Task<List<AttendanceSchedule>> GetActiveSchedules(GetComprehensiveAttendanceReportQuery query, CancellationToken cancellationToken)
    {
        IQueryable<AttendanceSchedule> scheduleQuery = context.AttendanceSchedules
            .AsNoTracking()
            .Include(s => s.Employee)
            .ThenInclude(e => e.OrganizationalUnit)
            .Include(s => s.ScheduleDays)
            .ThenInclude(sd => sd.Shift)
            .Include(s => s.Exceptions)
            .ThenInclude(e => e.Shift)
            .Where(s => s.IsActive &&
                       s.StartDate <= DateOnly.FromDateTime(query.EndDate) &&
                       (s.EndDate == null || s.EndDate >= DateOnly.FromDateTime(query.StartDate)))
            .AsSplitQuery(); // تقسيم الاستعلام لتحسين الأداء

        // تطبيق نفس الفلاتر
        if (query.OrganizationId.HasValue)
        {
            scheduleQuery = scheduleQuery.Where(s => s.Employee.OrganizationalUnitId == query.OrganizationId.Value);
        }

        if (query.OrganizationalUnitId.HasValue)
        {
            scheduleQuery = scheduleQuery.Where(s => s.Employee.OrganizationalUnitId == query.OrganizationalUnitId.Value);
        }

        if (query.EmployeeId.HasValue)
        {
            scheduleQuery = scheduleQuery.Where(s => s.EmployeeId == query.EmployeeId.Value);
        }

        if (query.ManagerId.HasValue)
        {
            scheduleQuery = scheduleQuery.Where(s => s.Employee.ManagerId == query.ManagerId.Value);
        }

        return await scheduleQuery.ToListAsync(cancellationToken);
    }

    private Task<Dictionary<Guid, Dictionary<DateTime, ExpectedWorkDay>>> CalculateExpectedWorkingDays(
        DateTime startDate, DateTime endDate,
        List<AttendanceSchedule> schedules)
    {
        Dictionary<Guid, Dictionary<DateTime, ExpectedWorkDay>> expectedWorkingDays = new();

        foreach (AttendanceSchedule schedule in schedules)
        {
            var employeeExpectedDays = new Dictionary<DateTime, ExpectedWorkDay>();
            expectedWorkingDays[schedule.EmployeeId] = employeeExpectedDays;

            // حساب الأيام المتوقعة من الجدول
            for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
            {
                var dateOnly = DateOnly.FromDateTime(date);

                // التحقق من أن التاريخ ضمن نطاق الجدول
                if (dateOnly < schedule.StartDate || schedule.EndDate.HasValue && dateOnly > schedule.EndDate.Value)
                {
                    continue;
                }

                // التحقق من أن التاريخ غير مستثنى
                if (schedule.ExcludedDates.Contains(dateOnly))
                {
                    continue;
                }

                // البحث عن استثناء لهذا التاريخ
                ScheduleIssue exception = schedule.Exceptions.FirstOrDefault(e => e.Date == dateOnly);

                // البحث عن يوم الجدول العادي
                ScheduleDay scheduleDay = schedule.ScheduleDays.FirstOrDefault(sd =>
                    sd.ScheduleDayDate == dateOnly && sd.IsActive);

                if (exception is not null)
                {
                    // استخدام الاستثناء
                    employeeExpectedDays[date] = new ExpectedWorkDay
                    {
                        Date = date,
                        ShiftId = exception.ShiftId,
                        ShiftName = exception.Shift.Name,
                        StartTime = exception.Shift.StartTime.ToTimeSpan(),
                        EndTime = exception.Shift.EndTime.ToTimeSpan(),
                        AttendanceScheduleId = schedule.Id,
                        ScheduleName = schedule.Notes ?? $"جدول {schedule.Id}"
                    };
                }
                else if (scheduleDay is not null)
                {
                    // استخدام الجدول العادي
                    employeeExpectedDays[date] = new ExpectedWorkDay
                    {
                        Date = date,
                        ShiftId = scheduleDay.ShiftId,
                        ShiftName = scheduleDay.Shift.Name,
                        StartTime = scheduleDay.Shift.StartTime.ToTimeSpan(),
                        EndTime = scheduleDay.Shift.EndTime.ToTimeSpan(),
                        AttendanceScheduleId = schedule.Id,
                        ScheduleName = schedule.Notes ?? $"جدول {schedule.Id}"
                    };
                }
                // إذا لم يجد استثناء أو يوم جدول عادي، لا يضيف اليوم المتوقع
            }
        }

        return Task.FromResult(expectedWorkingDays);
    }

    private static ComprehensiveAttendanceStatistics CalculateComprehensiveStatistics(
        List<Domain.Entities.Attendance.Attendance> attendances,
        Dictionary<Guid, Dictionary<DateTime, ExpectedWorkDay>> expectedWorkingDays)
    {
        IEnumerable<Guid> distinctEmployees = attendances.Select(a => a.EmployeeId).Distinct();
        IEnumerable<DateTime> distinctDates = attendances.Select(a => a.Date.Date).Distinct();

        // حساب إجمالي الأيام المتوقعة
        int totalExpectedWorkingDays = expectedWorkingDays.Values.Sum(employeeDays => employeeDays.Count);

        var statistics = new ComprehensiveAttendanceStatistics
        {
            TotalEmployees = distinctEmployees.Count(),
            TotalWorkingDays = distinctDates.Count(),
            TotalAttendanceRecords = attendances.Count,
            TotalExpectedWorkingDays = totalExpectedWorkingDays
        };

        // إحصائيات الحضور
        statistics.PresentEmployees = attendances.Count(a => a.Status == AttendanceStatus.Present);
        statistics.AbsentEmployees = attendances.Count(a => a.Status == AttendanceStatus.Absent);
        statistics.OnLeaveEmployees = attendances.Count(a => a.Status == AttendanceStatus.Vacation);
        statistics.AverageAttendanceRate = statistics.TotalAttendanceRecords > 0
            ? (decimal)statistics.PresentEmployees / statistics.TotalAttendanceRecords * 100
            : 0;

        // إحصائيات التأخير
        var lateAttendances = attendances.Where(a => a.LateMinutes > 0).ToList();
        statistics.LateArrivals = lateAttendances.Count;
        statistics.TotalLateMinutes = lateAttendances.Sum(a => a.LateMinutes ?? 0);
        statistics.AverageLateMinutes = statistics.LateArrivals > 0
            ? statistics.TotalLateMinutes / statistics.LateArrivals
            : 0;
        statistics.LateArrivalRate = statistics.TotalAttendanceRecords > 0
            ? (decimal)statistics.LateArrivals / statistics.TotalAttendanceRecords * 100
            : 0;

        // إحصائيات الانصراف المبكر
        var earlyDepartures = attendances.Where(a => a.EarlyLeaveMinutes > 0).ToList();
        statistics.EarlyDepartures = earlyDepartures.Count;
        statistics.TotalEarlyDepartureMinutes = earlyDepartures.Sum(a => a.EarlyLeaveMinutes ?? 0);
        statistics.AverageEarlyDepartureMinutes = statistics.EarlyDepartures > 0
            ? statistics.TotalEarlyDepartureMinutes / statistics.EarlyDepartures
            : 0;
        statistics.EarlyDepartureRate = statistics.TotalAttendanceRecords > 0
            ? (decimal)statistics.EarlyDepartures / statistics.TotalAttendanceRecords * 100
            : 0;

        // إحصائيات العمل الإضافي
        var overtimeAttendances = attendances.Where(a => a.OvertimeMinutes > 0).ToList();
        statistics.OvertimeEmployees = overtimeAttendances.Select(a => a.EmployeeId).Distinct().Count();
        statistics.TotalOvertimeHours = overtimeAttendances.Sum(a => a.OvertimeMinutes ?? 0) / 60.0m;
        statistics.AverageOvertimeHours = statistics.OvertimeEmployees > 0
            ? statistics.TotalOvertimeHours / statistics.OvertimeEmployees
            : 0;
        statistics.OvertimeRate = statistics.TotalAttendanceRecords > 0
            ? (decimal)overtimeAttendances.Count / statistics.TotalAttendanceRecords * 100
            : 0;

        // إحصائيات الإجازات الساعية
        var hourlyLeaves = attendances.SelectMany(a => a.Breaks.Where(b => b.BreakType == BreakType.Vacation)).ToList();
        statistics.HourlyLeaveEmployees = hourlyLeaves.Select(b => b.AttendanceId).Distinct().Count();
        statistics.TotalHourlyLeaveHours = hourlyLeaves.Sum(b => b.DurationMinutes) / 60.0m;
        statistics.AverageHourlyLeaveHours = statistics.HourlyLeaveEmployees > 0
            ? statistics.TotalHourlyLeaveHours / statistics.HourlyLeaveEmployees
            : 0;
        statistics.TotalHourlyLeaveRequests = hourlyLeaves.Count;

        // إحصائيات ساعات العمل
        statistics.TotalWorkingHours = attendances.Sum(a => a.WorkingMinutes ?? 0) / 60.0m;
        statistics.AverageWorkingHours = statistics.TotalAttendanceRecords > 0
            ? statistics.TotalWorkingHours / statistics.TotalAttendanceRecords
            : 0;
        statistics.TotalBreakHours = attendances.Sum(a => a.BreakMinutes ?? 0) / 60.0m;
        statistics.AverageBreakHours = statistics.TotalAttendanceRecords > 0
            ? statistics.TotalBreakHours / statistics.TotalAttendanceRecords
            : 0;

        return statistics;
    }

    private static List<ComprehensiveEmployeeSummary> GenerateComprehensiveEmployeeSummaries(
        List<Domain.Entities.Attendance.Attendance> attendances,
        Dictionary<Guid, Dictionary<DateTime, ExpectedWorkDay>> expectedWorkingDays)
    {
        // دمج الموظفين من الحضور
        IEnumerable<Guid> allEmployeeIds = attendances.Select(a => a.EmployeeId).Distinct();

        var summaries = new List<ComprehensiveEmployeeSummary>();

        foreach (Guid employeeId in allEmployeeIds)
        {
            var employeeAttendances = attendances.Where(a => a.EmployeeId == employeeId).ToList();
            Dictionary<DateTime, ExpectedWorkDay> employeeExpectedDays = expectedWorkingDays.GetValueOrDefault(employeeId, new Dictionary<DateTime, ExpectedWorkDay>());

            if (!employeeAttendances.Any())
            {
                continue;
            }

            Domain.Entities.Attendance.Attendance firstAttendance = employeeAttendances.FirstOrDefault();

            ComprehensiveEmployeeSummary summary = new()
            {
                EmployeeId = employeeId,
                FullName = firstAttendance?.Employee.FullName ?? string.Empty,
                Code = firstAttendance?.Employee.Code ?? string.Empty,
                DepartmentName = firstAttendance?.Employee.OrganizationalUnit?.UnitName ?? "غير محدد",

                // إحصائيات الحضور
                TotalWorkingDays = employeeAttendances.Count,
                ExpectedWorkingDays = employeeExpectedDays.Count,
                PresentDays = employeeAttendances.Count(a => a.Status == AttendanceStatus.Present),
                AbsentDays = employeeAttendances.Count(a => a.Status == AttendanceStatus.Absent),
                OnLeaveDays = employeeAttendances.Count(a => a.Status == AttendanceStatus.Vacation),
                AttendanceRate = employeeAttendances.Any() ? (decimal)employeeAttendances.Count(a => a.Status == AttendanceStatus.Present) / employeeAttendances.Count * 100 : 0,

                // إحصائيات التأخير
                LateArrivals = employeeAttendances.Count(a => a.LateMinutes > 0),
                TotalLateMinutes = employeeAttendances.Sum(a => a.LateMinutes ?? 0),
                AverageLateMinutes = employeeAttendances.Any(a => a.LateMinutes > 0)
                    ? (decimal)employeeAttendances.Where(a => a.LateMinutes > 0).Average(a => a.LateMinutes ?? 0)
                    : 0,
                LateArrivalRate = employeeAttendances.Any() ? (decimal)employeeAttendances.Count(a => a.LateMinutes > 0) / employeeAttendances.Count * 100 : 0,

                // إحصائيات الانصراف المبكر
                EarlyDepartures = employeeAttendances.Count(a => a.EarlyLeaveMinutes > 0),
                TotalEarlyDepartureMinutes = employeeAttendances.Sum(a => a.EarlyLeaveMinutes ?? 0),
                AverageEarlyDepartureMinutes = employeeAttendances.Any(a => a.EarlyLeaveMinutes > 0)
                    ? (decimal)employeeAttendances.Where(a => a.EarlyLeaveMinutes > 0).Average(a => a.EarlyLeaveMinutes ?? 0)
                    : 0,
                EarlyDepartureRate = employeeAttendances.Any() ? (decimal)employeeAttendances.Count(a => a.EarlyLeaveMinutes > 0) / employeeAttendances.Count * 100 : 0,

                // إحصائيات العمل الإضافي
                OvertimeDays = employeeAttendances.Count(a => a.OvertimeMinutes > 0),
                TotalOvertimeHours = employeeAttendances.Sum(a => a.OvertimeMinutes ?? 0) / 60.0m,
                AverageOvertimeHours = employeeAttendances.Any(a => a.OvertimeMinutes > 0)
                    ? (decimal)employeeAttendances.Where(a => a.OvertimeMinutes > 0).Average(a => a.OvertimeMinutes ?? 0) / 60.0m
                    : 0,
                OvertimeRate = employeeAttendances.Any() ? (decimal)employeeAttendances.Count(a => a.OvertimeMinutes > 0) / employeeAttendances.Count * 100 : 0,

                // إحصائيات الإجازات الساعية
                HourlyLeaveDays = employeeAttendances.SelectMany(a => a.Breaks.Where(b => b.BreakType == BreakType.Vacation)).Count(),
                TotalHourlyLeaveHours = employeeAttendances.SelectMany(a => a.Breaks.Where(b => b.BreakType == BreakType.Vacation)).Sum(b => b.DurationMinutes) / 60.0m,
                AverageHourlyLeaveHours = employeeAttendances.SelectMany(a => a.Breaks.Where(b => b.BreakType == BreakType.Vacation)).Any()
                    ? (decimal)employeeAttendances.SelectMany(a => a.Breaks.Where(b => b.BreakType == BreakType.Vacation)).Average(b => b.DurationMinutes) / 60.0m
                    : 0,
                HourlyLeaveRequests = employeeAttendances.SelectMany(a => a.Breaks.Where(b => b.BreakType == BreakType.Vacation)).Count(),

                // إحصائيات ساعات العمل
                TotalWorkingHours = employeeAttendances.Sum(a => a.WorkingMinutes ?? 0) / 60.0m,
                AverageWorkingHours = employeeAttendances.Any() ? (decimal)employeeAttendances.Average(a => a.WorkingMinutes ?? 0) / 60.0m : 0,
                TotalBreakHours = employeeAttendances.Sum(a => a.BreakMinutes ?? 0) / 60.0m,
                AverageBreakHours = employeeAttendances.Any() ? (decimal)employeeAttendances.Average(a => a.BreakMinutes ?? 0) / 60.0m : 0,

                // حساب درجة الأداء
                PerformanceScore = CalculatePerformanceScore(employeeAttendances),
                PerformanceLevel = GetPerformanceLevel(CalculatePerformanceScore(employeeAttendances))
            };

            summaries.Add(summary);
        }

        return summaries;
    }

    private static List<ComprehensiveDepartmentSummary> GenerateComprehensiveDepartmentSummaries(
        List<Domain.Entities.Attendance.Attendance> attendances)
    {
        var summaries = attendances
            .GroupBy(a => a.Employee.OrganizationalUnitId)
            .Select(g => new ComprehensiveDepartmentSummary
            {
                DepartmentId = g.Key ?? Guid.Empty,
                DepartmentName = g.First().Employee.OrganizationalUnit?.UnitName ?? "غير محدد",
                TotalEmployees = g.Select(a => a.EmployeeId).Distinct().Count(),

                // إحصائيات الحضور
                PresentEmployees = g.Where(a => a.Status == AttendanceStatus.Present).Select(a => a.EmployeeId).Distinct().Count(),
                AbsentEmployees = g.Where(a => a.Status == AttendanceStatus.Absent).Select(a => a.EmployeeId).Distinct().Count(),
                OnLeaveEmployees = g.Where(a => a.Status == AttendanceStatus.Vacation).Select(a => a.EmployeeId).Distinct().Count(),
                AverageAttendanceRate = g.Select(a => a.EmployeeId).Distinct().Any()
                    ? (decimal)g.Where(a => a.Status == AttendanceStatus.Present).Select(a => a.EmployeeId).Distinct().Count()
                        / g.Select(a => a.EmployeeId).Distinct().Count() * 100
                    : 0,

                // إحصائيات التأخير
                LateArrivals = g.Count(a => a.LateMinutes > 0),
                TotalLateMinutes = g.Sum(a => a.LateMinutes ?? 0),
                AverageLateMinutes = g.Any(a => a.LateMinutes > 0)
                    ? (decimal)g.Where(a => a.LateMinutes > 0).Average(a => a.LateMinutes ?? 0)
                    : 0,
                LateArrivalRate = g.Any() ? (decimal)g.Count(a => a.LateMinutes > 0) / g.Count() * 100 : 0,

                // إحصائيات الانصراف المبكر
                EarlyDepartures = g.Count(a => a.EarlyLeaveMinutes > 0),
                TotalEarlyDepartureMinutes = g.Sum(a => a.EarlyLeaveMinutes ?? 0),
                AverageEarlyDepartureMinutes = g.Any(a => a.EarlyLeaveMinutes > 0)
                    ? (decimal)g.Where(a => a.EarlyLeaveMinutes > 0).Average(a => a.EarlyLeaveMinutes ?? 0)
                    : 0,
                EarlyDepartureRate = g.Any() ? (decimal)g.Count(a => a.EarlyLeaveMinutes > 0) / g.Count() * 100 : 0,

                // إحصائيات العمل الإضافي
                OvertimeEmployees = g.Where(a => a.OvertimeMinutes > 0).Select(a => a.EmployeeId).Distinct().Count(),
                TotalOvertimeHours = g.Sum(a => a.OvertimeMinutes ?? 0) / 60.0m,
                AverageOvertimeHours = g.Any(a => a.OvertimeMinutes > 0)
                    ? (decimal)g.Where(a => a.OvertimeMinutes > 0).Average(a => a.OvertimeMinutes ?? 0) / 60.0m
                    : 0,
                OvertimeRate = g.Any() ? (decimal)g.Count(a => a.OvertimeMinutes > 0) / g.Count() * 100 : 0,

                // إحصائيات الإجازات الساعية
                HourlyLeaveEmployees = g.SelectMany(a => a.Breaks.Where(b => b.BreakType == BreakType.Vacation)).Select(b => b.AttendanceId).Distinct().Count(),
                TotalHourlyLeaveHours = g.SelectMany(a => a.Breaks.Where(b => b.BreakType == BreakType.Vacation)).Sum(b => b.DurationMinutes) / 60.0m,
                AverageHourlyLeaveHours = g.SelectMany(a => a.Breaks.Where(b => b.BreakType == BreakType.Vacation)).Any()
                    ? (decimal)g.SelectMany(a => a.Breaks.Where(b => b.BreakType == BreakType.Vacation)).Average(b => b.DurationMinutes) / 60.0m
                    : 0,
                TotalHourlyLeaveRequests = g.SelectMany(a => a.Breaks.Where(b => b.BreakType == BreakType.Vacation)).Count(),

                // إحصائيات ساعات العمل
                TotalWorkingHours = g.Sum(a => a.WorkingMinutes ?? 0) / 60.0m,
                AverageWorkingHours = g.Any() ? (decimal)g.Average(a => a.WorkingMinutes ?? 0) / 60.0m : 0,
                TotalBreakHours = g.Sum(a => a.BreakMinutes ?? 0) / 60.0m,
                AverageBreakHours = g.Any() ? (decimal)g.Average(a => a.BreakMinutes ?? 0) / 60.0m : 0,

                // أداء القسم
                DepartmentPerformanceScore = CalculateDepartmentPerformanceScore(g.ToList()),
                DepartmentPerformanceLevel = GetPerformanceLevel(CalculateDepartmentPerformanceScore(g.ToList()))
            })
            .ToList();

        return summaries;
    }

    private static List<DailyAttendanceSummary> GenerateDailySummaries(
        List<Domain.Entities.Attendance.Attendance> attendances,
        Dictionary<Guid, Dictionary<DateTime, ExpectedWorkDay>> expectedWorkingDays)
    {
        // دمج جميع التواريخ من الحضور
        IEnumerable<DateTime> allDates = attendances.Select(a => a.Date.Date)
            .Distinct()
            .OrderBy(d => d);

        var summaries = new List<DailyAttendanceSummary>();

        foreach (DateTime date in allDates)
        {
            var dayAttendances = attendances.Where(a => a.Date.Date == date).ToList();
            int dayExpectedEmployees = expectedWorkingDays.Values.Count(employeeDays => employeeDays.ContainsKey(date));

            DailyAttendanceSummary summary = new()
            {
                Date = date,
                DayName = date.ToString("dddd", new System.Globalization.CultureInfo("ar-SA")),
                TotalEmployees = dayAttendances.Select(a => a.EmployeeId).Distinct().Count(),
                ExpectedEmployees = dayExpectedEmployees,

                // إحصائيات الحضور
                PresentEmployees = dayAttendances.Where(a => a.Status == AttendanceStatus.Present).Select(a => a.EmployeeId).Distinct().Count(),
                AbsentEmployees = dayAttendances.Where(a => a.Status == AttendanceStatus.Absent).Select(a => a.EmployeeId).Distinct().Count(),
                OnLeaveEmployees = dayAttendances.Where(a => a.Status == AttendanceStatus.Vacation).Select(a => a.EmployeeId).Distinct().Count(),
                AttendanceRate = dayAttendances.Select(a => a.EmployeeId).Distinct().Any()
                    ? (decimal)dayAttendances.Where(a => a.Status == AttendanceStatus.Present).Select(a => a.EmployeeId).Distinct().Count()
                        / dayAttendances.Select(a => a.EmployeeId).Distinct().Count() * 100
                    : 0,

                // إحصائيات التأخير
                LateArrivals = dayAttendances.Count(a => a.LateMinutes > 0),
                TotalLateMinutes = dayAttendances.Sum(a => a.LateMinutes ?? 0),
                AverageLateMinutes = dayAttendances.Any(a => a.LateMinutes > 0)
                    ? (decimal)dayAttendances.Where(a => a.LateMinutes > 0).Average(a => a.LateMinutes ?? 0)
                    : 0,

                // إحصائيات الانصراف المبكر
                EarlyDepartures = dayAttendances.Count(a => a.EarlyLeaveMinutes > 0),
                TotalEarlyDepartureMinutes = dayAttendances.Sum(a => a.EarlyLeaveMinutes ?? 0),
                AverageEarlyDepartureMinutes = dayAttendances.Any(a => a.EarlyLeaveMinutes > 0)
                    ? (decimal)dayAttendances.Where(a => a.EarlyLeaveMinutes > 0).Average(a => a.EarlyLeaveMinutes ?? 0)
                    : 0,

                // إحصائيات العمل الإضافي
                OvertimeEmployees = dayAttendances.Where(a => a.OvertimeMinutes > 0).Select(a => a.EmployeeId).Distinct().Count(),
                TotalOvertimeHours = dayAttendances.Sum(a => a.OvertimeMinutes ?? 0) / 60.0m,
                AverageOvertimeHours = dayAttendances.Any(a => a.OvertimeMinutes > 0)
                    ? (decimal)dayAttendances.Where(a => a.OvertimeMinutes > 0).Average(a => a.OvertimeMinutes ?? 0) / 60.0m
                    : 0,

                // إحصائيات الإجازات الساعية
                HourlyLeaveEmployees = dayAttendances.SelectMany(a => a.Breaks.Where(b => b.BreakType == BreakType.Vacation)).Select(b => b.AttendanceId).Distinct().Count(),
                TotalHourlyLeaveHours = dayAttendances.SelectMany(a => a.Breaks.Where(b => b.BreakType == BreakType.Vacation)).Sum(b => b.DurationMinutes) / 60.0m,
                HourlyLeaveRequests = dayAttendances.SelectMany(a => a.Breaks.Where(b => b.BreakType == BreakType.Vacation)).Count(),

                // إحصائيات ساعات العمل
                TotalWorkingHours = dayAttendances.Sum(a => a.WorkingMinutes ?? 0) / 60.0m,
                AverageWorkingHours = dayAttendances.Any() ? (decimal)dayAttendances.Average(a => a.WorkingMinutes ?? 0) / 60.0m : 0,
                TotalBreakHours = dayAttendances.Sum(a => a.BreakMinutes ?? 0) / 60.0m,
                AverageBreakHours = dayAttendances.Any() ? (decimal)dayAttendances.Average(a => a.BreakMinutes ?? 0) / 60.0m : 0
            };

            summaries.Add(summary);
        }

        return summaries;
    }

    private static decimal CalculatePerformanceScore(List<Domain.Entities.Attendance.Attendance> attendances)
    {
        if (!attendances.Any())
        {
            return 0;
        }

        int totalDays = attendances.Count;
        int presentDays = attendances.Count(a => a.Status == AttendanceStatus.Present);
        int lateDays = attendances.Count(a => a.LateMinutes > 0);
        int earlyDepartureDays = attendances.Count(a => a.EarlyLeaveMinutes > 0);
        int overtimeDays = attendances.Count(a => a.OvertimeMinutes > 0);

        // حساب النقاط (100 نقطة كحد أقصى)
        decimal attendanceScore = (decimal)presentDays / totalDays * 40; // 40 نقطة للحضور
        decimal punctualityScore = (decimal)(totalDays - lateDays) / totalDays * 30; // 30 نقطة للدقة
        decimal completionScore = (decimal)(totalDays - earlyDepartureDays) / totalDays * 20; // 20 نقطة لإكمال الدوام
        decimal extraScore = overtimeDays > 0 ? 10 : 0; // 10 نقاط إضافية للعمل الإضافي

        return Math.Min(100, attendanceScore + punctualityScore + completionScore + extraScore);
    }

    private static decimal CalculateDepartmentPerformanceScore(List<Domain.Entities.Attendance.Attendance> attendances)
    {
        if (!attendances.Any())
        {
            return 0;
        }

        var employeeScores = attendances
            .GroupBy(a => a.EmployeeId)
            .Select(g => CalculatePerformanceScore(g.ToList()))
            .ToList();

        return employeeScores.Any() ? employeeScores.Average() : 0;
    }

    private static string GetPerformanceLevel(decimal score)
    {
        return score switch
        {
            >= 90 => "ممتاز",
            >= 80 => "جيد جداً",
            >= 70 => "جيد",
            >= 60 => "مقبول",
            _ => "ضعيف"
        };
    }
}

// Classes for internal data processing
internal sealed class ExpectedWorkDay
{
    public DateTime Date { get; set; }
    public Guid ShiftId { get; set; }
    public string ShiftName { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public Guid AttendanceScheduleId { get; set; }
    public string ScheduleName { get; set; } = string.Empty;
}
