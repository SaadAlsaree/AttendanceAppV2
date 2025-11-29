using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.BackgroundJobs;

public interface IAttendanceJob
{
    /// <summary>
    /// Create attendance records for all employees
    /// </summary>
    /// <param name="date">The date to create the attendance records for</param>
    /// <returns>Task representing the async operation</returns>
    Task CreateAttendanceRecordsAsync();
}
