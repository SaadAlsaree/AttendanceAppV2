using Domain.Entities.Attendance;
using Domain.Enums;
using Infrastructure.Database;
using Infrastructure.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

internal sealed class EmployeeExemptionService(
    ApplicationDbContext context,
    ILogger<EmployeeExemptionService> logger)
    : IEmployeeExemptionService
{
    public async Task<EmployeeExemption?> GetActiveExemptionAsync(Guid employeeId, DateOnly date)
    {
        try
        {
            // التحقق من وجود إجازة موافق عليها في هذا التاريخ
            Leave? leave = await context.Leaves
                .FirstOrDefaultAsync(l =>
                    l.EmployeeId == employeeId &&
                    l.Status == LeaveStatus.Approved &&
                    DateOnly.FromDateTime(l.StartDate) <= date &&
                    DateOnly.FromDateTime(l.EndDate) >= date);

            if (leave is not null)
            {
                return new EmployeeExemption
                {
                    EmployeeId = employeeId,
                    StartDate = leave.StartDate,
                    EndDate = leave.EndDate,
                    Reason = leave.Reason,
                    Type = MapLeaveTypeToExemptionType()
                };
            }

            // TODO: التحقق من أنواع الاستثناءات الأخرى مثل:
            // - التنسيب
            // - التكليف
            // - الواجب
            // - إجازات أخرى

            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting active exemption for Employee {EmployeeId} on {Date}", employeeId, date);
            return null;
        }
    }

    public async Task<List<EmployeeExemption>> GetActiveExemptionsForPeriodAsync(
        Guid employeeId,
        DateOnly startDate,
        DateOnly endDate)
    {
        try
        {
            List<EmployeeExemption> exemptions = new();

            // جلب جميع الإجازات الموافق عليها في هذه الفترة
            List<Leave> leaves = await context.Leaves
                .Where(l =>
                    l.EmployeeId == employeeId &&
                    l.Status == LeaveStatus.Approved &&
                    DateOnly.FromDateTime(l.StartDate) <= endDate &&
                    DateOnly.FromDateTime(l.EndDate) >= startDate)
                .ToListAsync();

            foreach (Leave leave in leaves)
            {
                exemptions.Add(new EmployeeExemption
                {
                    EmployeeId = employeeId,
                    StartDate = leave.StartDate,
                    EndDate = leave.EndDate,
                    Reason = leave.Reason,
                    Type = MapLeaveTypeToExemptionType()
                });
            }

            // TODO: إضافة أنواع استثناءات أخرى

            return exemptions;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting active exemptions for Employee {EmployeeId} for period {StartDate} to {EndDate}",
                employeeId, startDate, endDate);
            return new List<EmployeeExemption>();
        }
    }

    public async Task<bool> IsEmployeeExemptedAsync(Guid employeeId, DateOnly date)
    {
        EmployeeExemption? exemption = await GetActiveExemptionAsync(employeeId, date);
        return exemption is not null;
    }

    public async Task<(EmployeeExemption? Exemption, ExceptionType? Type)> GetExemptionWithTypeAsync(
        Guid employeeId,
        DateOnly date)
    {
        try
        {
            EmployeeExemption? exemption = await GetActiveExemptionAsync(employeeId, date);
            if (exemption is not null)
            {
                return (exemption, exemption.Type);
            }

            return (null, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting exemption with type for Employee {EmployeeId} on {Date}", employeeId, date);
            return (null, null);
        }
    }

    private static ExceptionType MapLeaveTypeToExemptionType()
    {
        // تحويل نوع الإجازة إلى نوع استثناء مناسب
        // يمكن تعديل هذا التحويل حسب متطلبات النظام
        // حالياً جميع أنواع الإجازات تُعاد كـ Holiday
        return ExceptionType.Holiday;
    }
}

/// <summary>
/// استثناء الموظف من الحضور
/// </summary>
public class EmployeeExemption
{
    public Guid EmployeeId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public ExceptionType Type { get; set; }
}

