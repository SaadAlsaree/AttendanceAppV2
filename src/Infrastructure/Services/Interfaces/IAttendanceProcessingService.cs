using Domain.Entities.Attendance;

namespace Infrastructure.Services.Interfaces;
public interface IAttendanceProcessingService
{


    /// <summary>
    /// إنشاء سجلات الحضور لجميع الموظفين إذا لم تكن موجودة
    /// </summary>
    Task CreateAttendanceRecordsAsyncIfNotExists();

    /// <summary>
    /// تحديث أوقات الدخول والخروج لجميع سجلات الحضور
    /// </summary>
    Task UpdateAttendancesCheckInAndCheckOutAsync();

    /// <summary>
    /// تحديث حالة الحضور لجميع سجلات الحضور
    /// </summary>
    Task UpdateAttendancesStatusAsync(Attendance attendance);

    /// <summary>
    /// تحديث سجلات الحضور إذا كان هناك إجازة أو استثناء
    /// </summary>
    Task UpdateAttendanceIfLeaveOrExceptionExistsAsync(Attendance attendance);

    /// <summary>
    /// تحديث المقاييس لجميع سجلات الحضور
    /// </summary>
    Task UpdateAttendanceMetricsAsync(Attendance attendance);

    /// <summary>
    /// تحديث الملاحظات لجميع سجلات الحضور
    /// </summary>
    Task UpdateAttendanceNotesAsync(Attendance attendance);

    /// <summary>
    /// حساب ساعات العمل الإضافية لجميع سجلات الحضور
    /// </summary>
    Task<decimal> CalculateOvertimeHoursAsync(Attendance attendance);

    /// <summary>
    /// حساب دقائق التأخير لجميع سجلات الحضور
    /// </summary>
    Task<int> CalculateLateMinutesAsync(Attendance attendance);
}
