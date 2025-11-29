using Hangfire;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundJobs;

/// <summary>
/// Service for scheduling and managing Hangfire jobs
/// </summary>
public class HangfireJobScheduler
{
    private readonly ILogger<HangfireJobScheduler> _logger;

    public HangfireJobScheduler(ILogger<HangfireJobScheduler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Schedule all recurring jobs for the attendance application
    /// </summary>
    public void ScheduleRecurringJobs()
    {
        try
        {
            _logger.LogInformation("Scheduling recurring Hangfire jobs");

            // Fetch attendance data from devices - runs every 5 minutes
            RecurringJob.AddOrUpdate<IFetchAttendanceDataJob>(
                "fetch-attendance-data",
                job => job.FetchAndSaveAttendanceDataAsync(),
                "*/5 * * * *", // Cron: Every 5 minutes
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Baghdad")
                });

            // Create attendance records for all employees - runs every 5 minutes (after fetch job)
            RecurringJob.AddOrUpdate<IAttendanceJob>(
                "create-attendance-records",
                job => job.CreateAttendanceRecordsAsync(),
                "*/5 * * * *", // Cron: Every 5 minutes
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Baghdad")
                });

            _logger.LogInformation("All recurring Hangfire jobs scheduled successfully");
        }
        catch (Exception ex)
        {
            const string message = "Error scheduling recurring Hangfire jobs";
            _logger.LogError(ex, message);
            throw new ApplicationException(message, ex);
        }
    }
    /// <summary>
    /// Schedule a delayed job
    /// </summary>
    public string ScheduleDelayedJob<T>(System.Linq.Expressions.Expression<Func<T, Task>> methodCall, TimeSpan delay)
    {
        string jobId = BackgroundJob.Schedule(methodCall, delay);
        _logger.LogInformation("Scheduled delayed job {JobId} with delay {Delay}", jobId, delay);
        return jobId;
    }
}
