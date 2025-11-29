namespace Infrastructure.BackgroundJobs;

/// <summary>
/// Interface for fetching attendance data from devices
/// </summary>
public interface IFetchAttendanceDataJob
{
    /// <summary>
    /// Fetch attendance data from all active devices and save to database
    /// </summary>
    /// <returns>Task representing the async operation</returns>
    Task FetchAndSaveAttendanceDataAsync();
}
