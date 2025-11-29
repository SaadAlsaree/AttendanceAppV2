using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Infrastructure.Database;
using Infrastructure.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundJobs;

public sealed class AttendanceJob(
    IAttendanceProcessingService processingService,
    ILogger<AttendanceJob> logger
) : IAttendanceJob
{

    public async Task CreateAttendanceRecordsAsync()
    {
        try
        {
            logger.LogInformation("Creating attendance records for all employees");
            await processingService.CreateAttendanceRecordsAsyncIfNotExists();
            await processingService.UpdateAttendancesCheckInAndCheckOutAsync();
            logger.LogInformation("Attendance records created successfully");

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating attendance records for all employees");
            throw new InvalidOperationException("Failed to create attendance records for all employees. See inner exception for details.", ex);
        }
    }
}
