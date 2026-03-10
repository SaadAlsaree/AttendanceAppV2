using Domain.Entities.Attendance;
using Domain.Models;
using Infrastructure.Database;
using Infrastructure.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Infrastructure.Services;

internal sealed class AttendanceDataSyncService(
    ExternalAttendanceDbContext externalContext,
    ApplicationDbContext context,
    IAttendanceValidationService validationService,
    IDateTimeProvider dateTimeProvider,
    ILogger<AttendanceDataSyncService> logger)
    : IAttendanceDataSyncService
{

    /// <summary>
    /// جلب ومعالجة البيانات الجديدة من قاعدة البيانات الخارجية
    /// </summary>
    public async Task<SyncResult> FetchAndProcessNewEventsAsync()
    {
        SyncResult result = new()
        {
            SyncStartedAt = DateTime.UtcNow,
            Success = false
        };

        try
        {
            logger.LogInformation("Starting attendance data sync process");

            // 1. تحديد نطاق التاريخ - جلب بيانات آخر 7 أيام
            DateTime now = DateTime.UtcNow;
            var todayStart = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
            var todayEnd = new DateTime(now.Year, now.Month, now.Day, 23, 59, 59, DateTimeKind.Utc);

            // جلب بيانات آخر 7 أيام (بما في ذلك اليوم الحالي)
            DateTime sevenDaysAgoStart = todayStart.AddDays(-1);

            var fromDateOnly = DateOnly.FromDateTime(sevenDaysAgoStart);
            var toDateOnly = DateOnly.FromDateTime(todayEnd);

            //logger.LogInformation("Fetching attendance data for today: {FromDate} to {ToDate}", fromDateOnly, toDateOnly);

            // 2. جلب الأحداث من قاعدة البيانات الخارجية
            List<EventTab> events = await FetchEventsFromExternalDatabaseAsync(fromDateOnly, toDateOnly);
            result.TotalRecordsFetched = events.Count;

            if (events.Count == 0)
            {
                logger.LogInformation("No new attendance events found");
                result.Success = true;
                result.SyncCompletedAt = DateTime.UtcNow;
                return result;
            }

            //logger.LogInformation("Fetched {Count} attendance events, processing with validation and bulk insert...", events.Count);

            // 3. التحقق من البيانات وتحويلها إلى سجلات حضور
            DateTime currentTimeUtc = DateTime.UtcNow;
            var validLogs = new List<AttendanceLog>();

            foreach (EventTab eventData in events)
            {
                try
                {
                    // التحقق من صحة البيانات
                    if (string.IsNullOrWhiteSpace(eventData.EmpID))
                    {
                        result.RecordsSkipped++;
                        result.Warnings.Add($"Skipped event with empty EmpID at {eventData.DateTimeAttend}");
                        //logger.LogDebug("Skipped event with empty EmpID at {DateTime}", eventData.DateTimeAttend);
                        continue;
                    }

                    DateTime dateTimeAttendUtc = dateTimeProvider.EnsureUtc(eventData.DateTimeAttend);
                    string deviceNo = eventData.DeviceNo ?? string.Empty;

                    // التحقق من عدم وجود تسجيل مكرر
                    bool isDuplicate = await validationService.IsDuplicateLogAsync(
                        eventData.EmpID,
                        dateTimeAttendUtc,
                        deviceNo);

                    if (isDuplicate)
                    {
                        result.RecordsSkipped++;
                        //logger.LogDebug("Skipped duplicate log for Employee {EmpID} at {DateTime} on Device {DeviceNo}",
                        //    eventData.EmpID, dateTimeAttendUtc, deviceNo);
                        continue;
                    }

                    // إنشاء سجل الحضور
                    AttendanceLog log = new()
                    {
                        Id = Guid.NewGuid(),
                        DateTimeAttend = dateTimeAttendUtc,
                        CardNo = eventData.CardNo ?? string.Empty,
                        EmpID = eventData.EmpID,
                        DateWork = eventData.DateWork,
                        TimeAttend = eventData.TimeAttend,
                        Direct = eventData.Direct,
                        DeviceName = eventData.DeviceName ?? string.Empty,
                        DeviceNo = deviceNo,
                        CreatedAt = currentTimeUtc,
                        LastUpdatedAt = currentTimeUtc
                    };

                    validLogs.Add(log);
                }
                catch (Exception ex)
                {
                    result.RecordsFailed++;
                    string errorMessage = $"Failed to process event for Employee {eventData.EmpID} at {eventData.DateTimeAttend}: {ex.Message}";
                    result.Errors.Add(errorMessage);
                    logger.LogError(ex, "Error processing attendance event for Employee {EmpID}", eventData.EmpID);
                }
            }

            if (validLogs.Count == 0)
            {
                logger.LogInformation("No valid attendance logs to insert after validation");
                result.Success = true;
                result.SyncCompletedAt = DateTime.UtcNow;
                return result;
            }

            // تحديث العدد الإجمالي للسجلات المسترجعة (قبل التحقق)
            result.TotalRecordsFetched = events.Count;

            // 4. Bulk Insert باستخدام batches لتحسين الأداء
            var efDbContext = context as Microsoft.EntityFrameworkCore.DbContext;
            if (efDbContext is not null)
            {
                efDbContext.ChangeTracker.AutoDetectChangesEnabled = false;
            }

            const int batchSize = 1000; // حجم الدفعة
            int totalProcessed = 0;

            for (int i = 0; i < validLogs.Count; i += batchSize)
            {
                try
                {
                    var batch = validLogs.Skip(i).Take(batchSize).ToList();
                    context.AttendanceLogs.AddRange(batch);
                    await context.SaveChangesAsync();

                    // Detach inserted entities to free memory
                    if (efDbContext is not null)
                    {
                        foreach (AttendanceLog log in batch)
                        {
                            Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry = efDbContext.Entry(log);
                            entry.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                        }
                    }

                    totalProcessed += batch.Count;
                    result.RecordsProcessedSuccessfully += batch.Count;
                    //logger.LogDebug("Processed batch {BatchNumber}, total processed: {TotalProcessed}/{Total}",
                    //    i / batchSize + 1, totalProcessed, validLogs.Count);
                }
                catch (Exception ex)
                {
                    result.RecordsFailed += Math.Min(batchSize, validLogs.Count - i);
                    string errorMessage = $"Failed to process batch starting at index {i}: {ex.Message}";
                    result.Errors.Add(errorMessage);
                    logger.LogError(ex, "Error processing batch starting at index {Index}", i);
                }
            }

            if (efDbContext is not null)
            {
                efDbContext.ChangeTracker.AutoDetectChangesEnabled = true;
            }

            result.Success = true;
            result.SyncCompletedAt = DateTime.UtcNow;

            //logger.LogInformation(
            //    "Sync completed successfully. Fetched: {Fetched}, Processed: {Processed}, Failed: {Failed}, Skipped: {Skipped}",
            //    result.TotalRecordsFetched,
            //    result.RecordsProcessedSuccessfully,
            //    result.RecordsFailed,
            //    result.RecordsSkipped);

            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.SyncCompletedAt = DateTime.UtcNow;
            logger.LogError(ex, "Fatal error during attendance data sync");
            throw new InvalidOperationException("Failed to sync attendance data. See inner exception for details.", ex);
        }
    }

    public async Task<List<EventTab>> FetchEventsFromExternalDatabaseAsync(DateOnly fromDate, DateOnly toDate)
    {
        try
        {


            List<EventTab> events = await externalContext.EventTabs
                .Where(e => e.DateWork >= fromDate && e.DateWork <= toDate)
                .ToListAsync();

            // إزالة التكرار: لكل موظف (EmpID) في كل يوم (DateWork)، سجل واحد فقط لكل Direct (1 و 2)
            // نأخذ أحدث سجل لكل مجموعة (بناءً على DateTimeAttend)
            var result = events
                .Select(e => new EventTab
                {
                    CardNo = (e.CardNo ?? string.Empty).Trim(),
                    EmpID = (e.EmpID ?? string.Empty).Trim(),
                    DateWork = e.DateWork,
                    TimeAttend = e.TimeAttend,
                    Direct = e.Direct,
                    DeviceName = (e.DeviceName ?? string.Empty).Trim(),
                    DeviceNo = (e.DeviceNo ?? string.Empty).Trim(),
                    EmpName = (e.EmpName ?? string.Empty).Trim(),
                    DateTimeAttend = e.DateTimeAttend,
                })
                .GroupBy(e => new { e.EmpID, e.DateWork, e.Direct })
                .Select(g => g.OrderByDescending(e => e.DateTimeAttend).First())
                .ToList();

            //logger.LogInformation("Fetched {TotalCount} events, after deduplication: {UniqueCount} unique records (EmpID + DateWork + Direct)",
            //    events.Count, result.Count);

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching events from external database for date range {FromDate} to {ToDate}", fromDate, toDate);
            throw new InvalidOperationException(
                $"Failed to fetch events from external database for date range {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}. See inner exception for details.",
                ex);
        }
    }

    public async Task<DateTime?> GetLastSuccessfulSyncTimeAsync()
    {
        try
        {
            // جلب آخر سجل تم إنشاؤه في جدول AttendanceLogs
            DateTime? lastSyncTime = await context.AttendanceLogs
                .Where(log => !log.IsDeleted)
                .OrderByDescending(log => log.CreatedAt)
                .Select(log => log.CreatedAt)
                .FirstOrDefaultAsync();

            if (lastSyncTime.HasValue)
            {
                //logger.LogInformation("Last successful sync time: {LastSyncTime}", lastSyncTime.Value);
            }
            else
            {
                logger.LogInformation("No previous sync records found");
            }

            return lastSyncTime;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting last successful sync time");
            return null;
        }
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            logger.LogInformation("Testing connection to external attendance database");

            // محاولة تنفيذ استعلام بسيط للتحقق من الاتصال
            bool canConnect = await externalContext.Database.CanConnectAsync();

            if (canConnect)
            {
                // محاولة جلب سجل واحد للتأكد من صحة الوصول إلى الجدول
                _ = await externalContext.EventTabs
                    .Take(1)
                    .FirstOrDefaultAsync();

                logger.LogInformation("Successfully connected to external attendance database");
                return true;
            }

            logger.LogWarning("Failed to connect to external attendance database");
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error testing connection to external attendance database");
            return false;
        }
    }
}
