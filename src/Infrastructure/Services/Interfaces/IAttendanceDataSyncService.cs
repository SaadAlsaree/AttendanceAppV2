using Domain.Models;

namespace Infrastructure.Services.Interfaces;
public interface IAttendanceDataSyncService
{
    /// <summary>
    /// جلب ومعالجة البيانات الجديدة من قاعدة البيانات الخارجية
    /// </summary>
    /// <param name="fromDate">جلب البيانات من هذا التاريخ (اختياري - يجلب من آخر مزامنة إذا كان null)</param>
    /// <param name="toDate">جلب البيانات حتى هذا التاريخ (اختياري - يستخدم الآن إذا كان null)</param>
    /// <returns>نتيجة عملية المزامنة</returns>
    Task<SyncResult> FetchAndProcessNewEventsAsync();

    /// <summary>
    /// الحصول على تاريخ آخر مزامنة ناجحة
    /// </summary>
    Task<DateTime?> GetLastSuccessfulSyncTimeAsync();

    /// <summary>
    /// التحقق من حالة الاتصال بقاعدة البيانات الخارجية
    /// </summary>
    Task<bool> TestConnectionAsync();

    /// <summary>
    /// جلب الأحداث من قاعدة البيانات الخارجية
    /// </summary>
    /// <param name="fromDate">تاريخ البداية</param>
    /// <param name="toDate">تاريخ النهاية</param>
    /// <returns>قائمة الأحداث</returns>
    Task<List<EventTab>> FetchEventsFromExternalDatabaseAsync(DateOnly fromDate, DateOnly toDate);

}


/// <summary>
/// نتيجة عملية المزامنة
/// </summary>
public class SyncResult
{
    public bool Success { get; set; }
    public int TotalRecordsFetched { get; set; }
    public int RecordsProcessedSuccessfully { get; set; }
    public int RecordsFailed { get; set; }
    public int RecordsSkipped { get; set; }
    public DateTime SyncStartedAt { get; set; }
    public DateTime? SyncCompletedAt { get; set; }
    public TimeSpan Duration => SyncCompletedAt.HasValue
        ? SyncCompletedAt.Value - SyncStartedAt
        : TimeSpan.Zero;
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public override string ToString()
    {
        return $"Sync Result: {(Success ? "SUCCESS" : "FAILED")} | " +
               $"Fetched: {TotalRecordsFetched} | " +
               $"Processed: {RecordsProcessedSuccessfully} | " +
               $"Failed: {RecordsFailed} | " +
               $"Skipped: {RecordsSkipped} | " +
               $"Duration: {Duration.TotalSeconds:F2}s";
    }
}
