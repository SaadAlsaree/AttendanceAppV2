using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Models;
using Domain.Entities.Organizations;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Reports.GetOrganizationalSummary;

internal sealed class GetOrganizationalSummaryHandler(
    IApplicationDbContext context,
    IUserContext userContext,
    IHasPermission hasPermission,
    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetOrganizationalSummaryQuery, ApiResponse<OrganizationalSummaryVm>>
{
    /// <summary>
    /// The units this request may report on, or <c>null</c> for an unscoped (Admin/SuperAdmin)
    /// caller. Every unit-set derivation below must pass through this, otherwise a scoped role
    /// reads the whole organization.
    /// </summary>
    private HashSet<Guid>? _scopedUnitIds;

    public async Task<Result<ApiResponse<OrganizationalSummaryVm>>> Handle(GetOrganizationalSummaryQuery query, CancellationToken cancellationToken)
    {
        try
        {
            UserInfoDto user = await userContext.GetUserAsync();

            // This handler had no role check at all: it loaded every unit and counted every
            // employee regardless of caller. Resolve the caller's scope up front.
            if (user.Role is not (Role.Admin or Role.SuperAdmin))
            {
                _scopedUnitIds = (await hasPermission.GetAccessibleUnitIdsAsync(cancellationToken)).ToHashSet();

                if (_scopedUnitIds.Count == 0)
                {
                    return Result.Failure<ApiResponse<OrganizationalSummaryVm>>(
                        Error.Forbidden("GetOrganizationalSummary.AccessDenied", "لا توجد وحدات تنظيمية ضمن صلاحيتك"));
                }
            }
            // Use provided date or default to the current local date (Today)
            DateTime rawDate = query.Date ?? dateTimeProvider.GetUtcNow().AddDays(-1);
            var reportDate = DateTime.SpecifyKind(rawDate, DateTimeKind.Utc);
            DateTime generatedAt = dateTimeProvider.GetUtcNow();

            // تحديد الوحدات المطلوبة
            var units = new List<OrganizationalUnit>();
            if (query.OrganizationalUnitId.HasValue)
            {
                // A scoped caller may only ask about a unit inside their own scope.
                if (_scopedUnitIds is not null && !_scopedUnitIds.Contains(query.OrganizationalUnitId.Value))
                {
                    return Result.Failure<ApiResponse<OrganizationalSummaryVm>>(
                        Error.Forbidden("GetOrganizationalSummary.AccessDenied", "لا تملك صلاحية على هذه الوحدة"));
                }

                if (query.IncludeSubUnits)
                {
                    // الحصول على الوحدة وجميع الوحدات الفرعية
                    units = await GetUnitWithSubUnits(user.OrganizationalUnitId ?? query.OrganizationalUnitId.Value, cancellationToken);
                }
                else
                {
                    // الحصول على الوحدة فقط
                    OrganizationalUnit? unit = await context.OrganizationalUnits
                        .FirstOrDefaultAsync(u => u.Id == user.OrganizationalUnitId, cancellationToken);
                    if (unit is not null)
                    {
                        units.Add(unit);
                    }
                }
            }
            else if (_scopedUnitIds is not null)
            {
                units = await context.OrganizationalUnits
                    .Where(u => _scopedUnitIds.Contains(u.Id) && !u.IsDeleted)
                    .ToListAsync(cancellationToken);
            }
            else
            {
                // الحصول على جميع الوحدات
                units = await context.OrganizationalUnits.ToListAsync(cancellationToken);
            }

            // فلترة الوحدات التي يكون مستواها بين 1-3.
            // Skipped for a scoped caller: their scope is already the constraint, and a site may
            // legitimately contain a level-4 unit, which this filter would silently drop.
            if (_scopedUnitIds is null)
            {
                units = units.Where(u => u.UnitLevel.HasValue && u.UnitLevel.Value >= 1 && u.UnitLevel.Value <= 3).ToList();
            }

            if (!units.Any())
            {
                return Result.Failure<ApiResponse<OrganizationalSummaryVm>>(
                    Error.NotFound("GetOrganizationalSummary.NoUnits", "لا توجد وحدات تنظيمية"));
            }

            var unitIds = units.Select(u => u.Id).ToList();

            // إحصائيات عامة
            // عدد الموظفين — محصور بالوحدات المشمولة بالتقرير.
            // This was an unfiltered global CountAsync for every caller, which leaked the whole
            // organization's headcount into an otherwise unit-scoped report.
            int totalEmployees = await context.Employees
                .Where(e => unitIds.Contains(e.OrganizationalUnitId ?? Guid.Empty))
                .CountAsync(cancellationToken);

            // The four totals below used to test `unitIds.Contains(user.OrganizationalUnitId)` —
            // a constant per request rather than a per-row filter, so they evaluated to 0 whenever
            // the caller's own unit was not itself in the report (always, for a caller with no
            // unit). They now filter on the row's own employee unit, matching BuildUnitSummary.

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

        // A scoped caller must never see a descendant that is outside their scope. For a site,
        // whose membership is non-transitive, this reduces the walk to the member units only.
        if (_scopedUnitIds is not null)
        {
            result = result.Where(u => _scopedUnitIds.Contains(u.Id)).ToList();
        }

        return result;
    }

    private async Task<UnitSummary> BuildUnitSummary(OrganizationalUnit unit, DateTime reportDate, CancellationToken cancellationToken)
    {
        // الحصول على جميع معرفات الوحدات الفرعية (بما في ذلك الوحدة نفسها)
        List<Guid> unitIds = await GetUnitWithAllSubUnitsIds(unit.Id, cancellationToken);

        // إحصائيات الموظفين في الوحدة والوحدات الفرعية
        int unitEmployees = await context.Employees
            .Where(e => unitIds.Contains(e.OrganizationalUnitId ?? Guid.Empty))
            .CountAsync(cancellationToken);

        // إحصائيات الحضور في الوحدة والوحدات الفرعية
        int unitAttendances = await context.Attendances
            .Where(a => a.Date.Date == reportDate &&
                       unitIds.Contains(a.Employee.OrganizationalUnitId ?? Guid.Empty) &&
                       (a.CheckInTime != null || a.CheckOutTime != null))
            .CountAsync(cancellationToken);

        // إحصائيات الإجازات في الوحدة والوحدات الفرعية
        int unitLeaves = await context.Leaves
            .Where(l => l.StartDate.Date <= reportDate &&
                       l.EndDate.Date >= reportDate &&
                       unitIds.Contains(l.Employee.OrganizationalUnitId ?? Guid.Empty))
            .CountAsync(cancellationToken);

        // عدد غير المبصمين الذين لديهم ShiftId لهذا اليوم
        // جلب معرفات الموظفين في إجازة لهذا اليوم في الوحدة والوحدات الفرعية
        List<Guid> employeesOnLeave = await context.Leaves
            .Where(l => l.StartDate.Date <= reportDate &&
                       l.EndDate.Date >= reportDate &&
                       unitIds.Contains(l.Employee.OrganizationalUnitId ?? Guid.Empty))
            .Select(l => l.EmployeeId)
            .ToListAsync(cancellationToken);

        int unitNotAttendances = await context.Attendances
            .Where(a => a.Date.Date == reportDate &&
                       unitIds.Contains(a.Employee.OrganizationalUnitId ?? Guid.Empty) &&
                       a.ShiftId != null &&
                       a.CheckInTime == null &&
                       a.CheckOutTime == null &&
                       !employeesOnLeave.Contains(a.EmployeeId))
            .CountAsync(cancellationToken);

        // إحصائيات التأخير في الوحدة والوحدات الفرعية
        int unitLate = await context.Attendances
            .Where(a => a.Date.Date == reportDate &&
                       a.Status == AttendanceStatus.Late &&
                       unitIds.Contains(a.Employee.OrganizationalUnitId ?? Guid.Empty) &&
                       (a.CheckInTime != null || a.CheckOutTime != null))
            .CountAsync(cancellationToken);

        // إحصائيات العمل الإضافي في الوحدة والوحدات الفرعية
        int unitOvertime = await context.Attendances
            .Where(a => a.Date.Date == reportDate &&
                       a.Status == AttendanceStatus.Overtime &&
                       unitIds.Contains(a.Employee.OrganizationalUnitId ?? Guid.Empty) &&
                       (a.CheckInTime != null || a.CheckOutTime != null))
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

    private async Task<List<Guid>> GetUnitWithAllSubUnitsIds(Guid unitId, CancellationToken cancellationToken)
    {
        List<OrganizationalUnit> allUnits = await context.OrganizationalUnits.ToListAsync(cancellationToken);
        List<Guid> result = new();

        void AddUnitAndChildren(Guid parentId)
        {
            OrganizationalUnit? unit = allUnits.FirstOrDefault(u => u.Id == parentId);
            if (unit is not null)
            {
                result.Add(unit.Id);
                var children = allUnits.Where(u => u.ParentUnitId == parentId).ToList();
                foreach (OrganizationalUnit child in children)
                {
                    AddUnitAndChildren(child.Id);
                }
            }
        }

        AddUnitAndChildren(unitId);

        // Same rule as GetUnitWithSubUnits: per-unit statistics must not roll up employees from
        // descendants the caller cannot see.
        if (_scopedUnitIds is not null)
        {
            result = result.Where(_scopedUnitIds.Contains).ToList();
        }

        return result;
    }
}
