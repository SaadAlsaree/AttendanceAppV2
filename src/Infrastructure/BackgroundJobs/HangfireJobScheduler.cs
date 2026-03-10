using System.Linq.Expressions;
using Hangfire;
using Hangfire.Common;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundJobs;

/// <summary>
/// Service for scheduling and managing Hangfire jobs
/// </summary>
public class HangfireJobScheduler
{
    private readonly ILogger<HangfireJobScheduler> _logger;
    private readonly IRecurringJobManager _recurringJobManager;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public HangfireJobScheduler(
        ILogger<HangfireJobScheduler> logger,
        IRecurringJobManager recurringJobManager,
        IBackgroundJobClient backgroundJobClient)
    {
        _logger = logger;
        _recurringJobManager = recurringJobManager;
        _backgroundJobClient = backgroundJobClient;
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
            var fetchJob = Job.FromExpression<IFetchAttendanceDataJob>(job => job.FetchAndSaveAttendanceDataAsync());
            _recurringJobManager.AddOrUpdate(
                "fetch-attendance-data",
                fetchJob,
                "*/5 * * * *",
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Baghdad")
                });

            // Create attendance records for all employees - runs every 5 minutes (after fetch job)
            var createAttendanceJob = Job.FromExpression<IAttendanceJob>(job => job.CreateAttendanceRecordsAsync());
            _recurringJobManager.AddOrUpdate(
                "create-attendance-records",
                createAttendanceJob,
                "*/5 * * * *",
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
    public string ScheduleDelayedJob<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay)
    {
        string jobId = _backgroundJobClient.Schedule(methodCall, delay);
        //_logger.LogInformation("Scheduled delayed job {JobId} with delay {Delay}", jobId, delay);
        return jobId;
    }
}
