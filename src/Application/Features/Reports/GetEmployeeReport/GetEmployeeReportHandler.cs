using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Reports.GetEmployeeReport;

internal sealed class GetEmployeeReportHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetEmployeeReportQuery, ApiResponse<GetEmployeeReportVm>>
{
    public async Task<Result<ApiResponse<GetEmployeeReportVm>>> Handle(GetEmployeeReportQuery query, CancellationToken cancellationToken)
    {
        if (query.FromDate > query.ToDate)
        {
            return Result.Failure<ApiResponse<GetEmployeeReportVm>>(
                Error.Problem("Report.InvalidDateRange", "تاريخ البداية يجب أن يكون قبل تاريخ النهاية"));
        }

        Domain.Entities.Organizations.Employee? employee = await context.Employees
            .AsNoTracking()
            .Include(e => e.OrganizationalUnit)
            .FirstOrDefaultAsync(e => e.Id == query.EmployeeId, cancellationToken);

        if (employee is null)
        {
            return Result.Failure<ApiResponse<GetEmployeeReportVm>>(
                Error.NotFound("Employee.NotFound", "الموظف غير موجود"));
        }

        DateTime start = dateTimeProvider.EnsureUtc(query.FromDate.ToDateTime(TimeOnly.MinValue));
        DateTime end = dateTimeProvider.EnsureUtc(query.ToDate.ToDateTime(TimeOnly.MaxValue));

        List<Domain.Entities.Attendance.Attendance> attendances = await context.Attendances
            .AsNoTracking()
            .Where(a => a.EmployeeId == query.EmployeeId && a.Date >= start && a.Date <= end)
            .OrderBy(a => a.Date)
            .ToListAsync(cancellationToken);

        var vm = new GetEmployeeReportVm
        {
            EmployeeId = employee.Id,
            EmployeeName = employee.FullName,
            EmployeeCode = employee.Code ?? employee.EmpID,
            OrganizationalUnitName = employee.OrganizationalUnit?.UnitName ?? string.Empty,
            FromDate = start,
            ToDate = end,
            GeneratedAt = dateTimeProvider.GetCurrentLocalTime(),
            Days = attendances.Select(a => new EmployeeReportDay
            {
                Date = a.Date,
                CheckInTime = a.CheckInTime,
                CheckOutTime = a.CheckOutTime,
                StatusName = GetEnumDisplayName(a.Status),
                LateMinutes = a.LateMinutes ?? 0,
                EarlyLeaveMinutes = a.EarlyLeaveMinutes ?? 0,
                OvertimeMinutes = a.OvertimeMinutes ?? 0,
                IsNonFingerprinted = a.CheckInTime == null && a.CheckOutTime == null
            }).ToList()
        };

        vm.TotalDays = attendances.Count;
        vm.PresentDays = attendances.Count(a => a.Status == AttendanceStatus.Present);
        vm.AbsentDays = attendances.Count(a => a.Status == AttendanceStatus.Absent);
        vm.LateDays = attendances.Count(a => a.LateMinutes > 0);
        vm.EarlyLeaveDays = attendances.Count(a => a.EarlyLeaveMinutes > 0);
        vm.LeaveDays = attendances.Count(a => a.Status == AttendanceStatus.Vacation);
        vm.TotalOvertimeHours = Math.Round(attendances.Sum(a => a.OvertimeMinutes ?? 0) / 60.0, 2);

        return Result.Success(new ApiResponse<GetEmployeeReportVm>
        {
            Data = vm,
            Message = "تم إنشاء تقرير الموظف بنجاح",
            IsSuccess = true
        });
    }

    // Resolve an enum value's [Display(Name = "...")] Arabic label, falling back to the enum name.
    private static string GetEnumDisplayName<TEnum>(TEnum value) where TEnum : struct, Enum
    {
        DisplayAttribute? displayAttribute = typeof(TEnum)
            .GetField(value.ToString())
            ?.GetCustomAttribute<DisplayAttribute>();

        return displayAttribute?.Name ?? value.ToString();
    }
}
