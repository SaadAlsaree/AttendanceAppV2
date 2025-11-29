using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities.Attendance;

namespace Domain.Models;

public class EventTab
{
    public DateTime DateTimeAttend { get; set; }
    public string CardNo { get; set; } = string.Empty;
    public string EmpID { get; set; } = string.Empty;
    public DateOnly DateWork { get; set; }
    public TimeSpan? TimeAttend { get; set; }  // Assuming this stores only time
    public int Direct { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceNo { get; set; } = string.Empty;
    public string EmpName { get; set; } = string.Empty;
}
