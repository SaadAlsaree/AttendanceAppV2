using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.BackgroundJobs;

public static class HangfireConfiguration
{
    public static IServiceCollection AddHangfire(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Get connection string for Hangfire database
        string connectionString = configuration.GetConnectionString("Database")!;

        // Configure Hangfire to use PostgreSQL
        services.AddHangfire(config =>
        {
            config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(
                    opts => opts.UseNpgsqlConnection(connectionString),
                    new PostgreSqlStorageOptions
                    {
                        // Use a separate schema for Hangfire tables
                        SchemaName = "hangfire",

                        // Connection pool settings
                        PrepareSchemaIfNecessary = true,

                        // Job expiration settings
                        JobExpirationCheckInterval = TimeSpan.FromHours(1),
                        CountersAggregateInterval = TimeSpan.FromMinutes(5),

                        // Queue poll interval
                        QueuePollInterval = TimeSpan.FromSeconds(15),

                        // Use UTC for all timestamps
                        UseNativeDatabaseTransactions = true,

                        // Enable distributed locks
                        DistributedLockTimeout = TimeSpan.FromMinutes(10)
                    });
        });

        // Add Hangfire server with custom configuration
        services.AddHangfireServer(options =>
        {
            // Server name for identification in dashboard
            options.ServerName = Environment.MachineName + "-attendance-app";

            // Worker count based on CPU cores
            options.WorkerCount = Math.Max(Environment.ProcessorCount, 2);

            // Queues to process (default queue + specific queues)
            options.Queues = new[] { "critical", "default", "background", "reports" };

            // Heartbeat interval
            options.HeartbeatInterval = TimeSpan.FromSeconds(30);

            // Server check interval
            options.ServerCheckInterval = TimeSpan.FromMinutes(1);

            // Server timeout
            options.ServerTimeout = TimeSpan.FromMinutes(5);

            // Schedule polling interval
            options.SchedulePollingInterval = TimeSpan.FromSeconds(15);
        });

        return services;
    }

    public static IServiceCollection AddHangfireJobs(this IServiceCollection services)
    {
        // Register job services here
        // These will be the actual job implementations

        services.AddScoped<IFetchAttendanceDataJob, FetchAttendanceDataJob>();
        services.AddScoped<IAttendanceJob, AttendanceJob>();

        // Register job scheduler
        services.AddScoped<HangfireJobScheduler>();

        return services;
    }
}
