using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Organizations;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Reports.GetOrganizationalSummary;

internal sealed class GetOrganizationalSummaryHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetOrganizationalSummaryQuery, ApiResponse<OrganizationalSummaryVm>>
{
    public async Task<Result<ApiResponse<OrganizationalSummaryVm>>> Handle(GetOrganizationalSummaryQuery query, CancellationToken cancellationToken)
    {
        try
        {
            DateTime reportDate = query.Date ?? dateTimeProvider.GetUtcNow().Date;
            DateTime generatedAt = dateTimeProvider.GetUtcNow();

            // تحديد الوحدات المطلوبة
            var units = new List<OrganizationalUnit>();
            if (query.OrganizationalUnitId.HasValue)
            {
                if (query.IncludeSubUnits)
                {
                    // الحصول على الوحدة وجميع الوحدات الفرعية
                    units = await GetUnitWithSubUnits(query.OrganizationalUnitId.Value, cancellationToken);
                }
                else
                {
                    // الحصول على الوحدة فقط
                    OrganizationalUnit? unit = await context.OrganizationalUnits
                        .FirstOrDefaultAsync(u => u.Id == query.OrganizationalUnitId.Value, cancellationToken);
                    if (unit is not null)
                    {
                        units.Add(unit);
                    }
                }
            }
            else
            {
                // الحصول على جميع الوحدات
                units = await context.OrganizationalUnits.ToListAsync(cancellationToken);
            }

            // فلترة الوحدات التي يكون مستواها بين 1-3
            units = units.Where(u => u.UnitLevel.HasValue && u.UnitLevel.Value >= 1 && u.UnitLevel.Value <= 3).ToList();

            if (!units.Any())
            {
                return Result.Failure<ApiResponse<OrganizationalSummaryVm>>(
                    Error.NotFound("GetOrganizationalSummary.NoUnits", "لا توجد وحدات تنظيمية"));
            }

            var unitIds = units.Select(u => u.Id).ToList();

            // إحصائيات عامة
            // عدد الموظفين
            int totalEmployees = await context.Employees
                .Where(e => unitIds.Contains(e.OrganizationalUnitId ?? Guid.Empty))
                .CountAsync(cancellationToken);

            // عدد الحضور
            int totalAttendances = await context.Attendances
                .Where(a => a.Date.Date == reportDate &&
                           unitIds.Contains(a.Employee.OrganizationalUnitId ?? Guid.Empty) &&
                           (a.CheckInTime != null || a.CheckOutTime != null))
                .CountAsync(cancellationToken);

            // عدد الإجازات
            int totalLeaves = await context.Leaves
                .Where(l => l.StartDate.Date <= reportDate &&
                           l.EndDate.Date >= reportDate &&
                           unitIds.Contains(l.Employee.OrganizationalUnitId ?? Guid.Empty))
                .CountAsync(cancellationToken);

            int totalNotAttendances = totalEmployees - totalAttendances - totalLeaves;

            // عدد التأخير
            int totalLate = await context.Attendances
                .Where(a => a.Date.Date == reportDate &&
                           a.Status == AttendanceStatus.Late &&
                           unitIds.Contains(a.Employee.OrganizationalUnitId ?? Guid.Empty) &&
                           (a.CheckInTime != null || a.CheckOutTime != null))
                .CountAsync(cancellationToken);

            // عدد العمل الإضافي
            int totalOvertime = await context.Attendances
            .Where(a => a.Date.Date == reportDate &&
                       a.Status == AttendanceStatus.Overtime &&
                       unitIds.Contains(a.Employee.OrganizationalUnitId ?? Guid.Empty) &&
                       (a.CheckInTime != null || a.CheckOutTime != null))
            .CountAsync(cancellationToken);

            // بناء ملخص الوحدات
            var unitSummaries = new List<UnitSummary>();
            foreach (OrganizationalUnit unit in units)
            {
                UnitSummary unitSummary = await BuildUnitSummary(unit, reportDate, cancellationToken);
                unitSummaries.Add(unitSummary);
            }

            // بناء النتيجة النهائية
            OrganizationalSummaryVm result = new()
            {
                Date = reportDate,
                GeneratedAt = generatedAt,
                TotaleEmployees = totalEmployees,
                TottalAttendances = totalAttendances,
                TottalNotAttendances = totalNotAttendances,
                TotalLate = totalLate,
                TotalLeaves = totalLeaves,
                TotalOvertime = totalOvertime,
                Units = unitSummaries
            };

            return Result.Success(new ApiResponse<OrganizationalSummaryVm>
            {
                Data = result,
                Message = "تم إنشاء التقرير التنظيمي بنجاح"
            });
        }
        catch (Exception ex)
        {
            return Result.Failure<ApiResponse<OrganizationalSummaryVm>>(
                Error.Failure("GetOrganizationalSummary.Failed", ex.Message));
        }
    }

    private async Task<List<OrganizationalUnit>> GetUnitWithSubUnits(Guid unitId, CancellationToken cancellationToken)
    {
        List<OrganizationalUnit> allUnits = await context.OrganizationalUnits.ToListAsync(cancellationToken);
        List<OrganizationalUnit> result = new();

        void AddUnitAndChildren(Guid parentId)
        {
            OrganizationalUnit? unit = allUnits.FirstOrDefault(u => u.Id == parentId);
            if (unit is not null)
            {
                result.Add(unit);
                var children = allUnits.Where(u => u.ParentUnitId == parentId).ToList();
                foreach (OrganizationalUnit child in children)
                {
                    AddUnitAndChildren(child.Id);
                }
            }
        }

        AddUnitAndChildren(unitId);
        return result;
    }

    private async Task<UnitSummary> BuildUnitSummary(OrganizationalUnit unit, DateTime reportDate, CancellationToken cancellationToken)
    {
        // إحصائيات الموظفين في الوحدة
        int unitEmployees = await context.Employees
            .Where(e => e.OrganizationalUnitId == unit.Id)
            .CountAsync(cancellationToken);

        // إحصائيات الحضور في الوحدة
        int unitAttendances = await context.Attendances
            .Where(a => a.Date.Date == reportDate.Date && a.Employee.OrganizationalUnitId == unit.Id)
            .Where(a => a.CheckInTime != null || a.CheckOutTime != null)
            .CountAsync(cancellationToken);

        // إحصائيات الإجازات في الوحدة
        int unitLeaves = await context.Leaves
            .Where(l => l.StartDate.Date <= reportDate.Date &&
                       l.EndDate.Date >= reportDate.Date &&
                       l.Employee.OrganizationalUnitId == unit.Id)
            .CountAsync(cancellationToken);

        // عدد غير المبصمين الذين لديهم ShiftId لهذا اليوم
        // جلب معرفات الموظفين في إجازة لهذا اليوم
        List<Guid> employeesOnLeave = await context.Leaves
            .Where(l => l.StartDate.Date <= reportDate.Date &&
                       l.EndDate.Date >= reportDate.Date &&
                       l.Employee.OrganizationalUnitId == unit.Id)
            .Select(l => l.EmployeeId)
            .ToListAsync(cancellationToken);

        int unitNotAttendances = await context.Attendances
            .Where(a => a.Date.Date == reportDate.Date &&
                       a.Employee.OrganizationalUnitId == unit.Id &&
                       a.ShiftId != null &&
                       a.CheckInTime == null &&
                       a.CheckOutTime == null &&
                       !employeesOnLeave.Contains(a.EmployeeId))
            .CountAsync(cancellationToken);

        // إحصائيات التأخير في الوحدة
        int unitLate = await context.Attendances
            .Where(a => a.Date.Date == reportDate.Date &&
                       a.Status == AttendanceStatus.Late &&
                       a.Employee.OrganizationalUnitId == unit.Id)
            .CountAsync(cancellationToken);

        // إحصائيات العمل الإضافي في الوحدة
        int unitOvertime = await context.Attendances
            .Where(a => a.Date.Date == reportDate.Date &&
                       a.Status == AttendanceStatus.Overtime &&
                       a.Employee.OrganizationalUnitId == unit.Id)
            .CountAsync(cancellationToken);

        // إحصائيات المناوبات في الوحدة
        int unitShifts = await context.Shifts

            .CountAsync(cancellationToken);

        // الحصول على اسم الوحدة الأب
        string? parentUnitName = null;
        if (unit.ParentUnitId.HasValue)
        {
            OrganizationalUnit? parentUnit = await context.OrganizationalUnits
                .FirstOrDefaultAsync(u => u.Id == unit.ParentUnitId.Value, cancellationToken);
            parentUnitName = parentUnit?.UnitName;
        }

        return new UnitSummary
        {
            UnitId = unit.Id,
            UnitName = unit.UnitName,
            UnitCode = unit.UnitCode,
            ParentUnitId = unit.ParentUnitId,
            ParentUnitName = parentUnitName,
            TotalEmployees = unitEmployees,
            TotalShifts = unitShifts,
            TottalAttendances = unitAttendances,
            TottalNotAttendances = unitNotAttendances,
            TotalLate = unitLate,
            TotalLeaves = unitLeaves,
            TotalOvertime = unitOvertime
        };
    }
}
