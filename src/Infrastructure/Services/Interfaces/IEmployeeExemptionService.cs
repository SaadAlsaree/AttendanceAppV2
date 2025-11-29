using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Enums;

namespace Infrastructure.Services.Interfaces;

public interface IEmployeeExemptionService
{
    /// <summary>
    /// التحقق من وجود استثناء نشط للموظف في تاريخ معين
    /// </summary>
    Task<EmployeeExemption?> GetActiveExemptionAsync(Guid employeeId, DateOnly date);

    /// <summary>
    /// التحقق من وجود أي استثناء نشط للموظف في فترة محددة
    /// </summary>
    Task<List<EmployeeExemption>> GetActiveExemptionsForPeriodAsync(
        Guid employeeId,
        DateOnly startDate,
        DateOnly endDate);

    /// <summary>
    /// التحقق إذا كان الموظف مستثنى من البصمة في تاريخ معين
    /// </summary>
    Task<bool> IsEmployeeExemptedAsync(Guid employeeId, DateOnly date);

    /// <summary>
    /// الحصول على تفاصيل الاستثناء مع النوع
    /// </summary>
    Task<(EmployeeExemption? Exemption, ExceptionType? Type)> GetExemptionWithTypeAsync(
        Guid employeeId,
        DateOnly date);
}
