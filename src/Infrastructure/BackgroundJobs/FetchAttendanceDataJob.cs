using Hangfire;
using Infrastructure.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundJobs;

/// <summary>
/// Background job for fetching attendance data from external database
/// </summary>
internal sealed class FetchAttendanceDataJob(
    IAttendanceDataSyncService syncService,
    ILogger<FetchAttendanceDataJob> logger) : IFetchAttendanceDataJob
{
    public async Task FetchAndSaveAttendanceDataAsync()
    {
        try
        {
            logger.LogInformation("Starting scheduled attendance data fetch job");

            // استدعاء خدمة المزامنة لجلب ومعالجة البيانات
            SyncResult result = await syncService.FetchAndProcessNewEventsAsync();

            if (result.Success)
            {
                logger.LogInformation(
                    "Attendance data fetch job completed successfully. {Result}",
                    result.ToString());
            }
            else
            {
                logger.LogWarning(
                    "Attendance data fetch job completed with issues. {Result}",
                    result.ToString());

                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    logger.LogError("Error message: {ErrorMessage}", result.ErrorMessage);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fatal error in scheduled attendance data fetch job");
            throw new InvalidOperationException("Failed to fetch attendance data. See inner exception for details.", ex);
        }
    }
}
