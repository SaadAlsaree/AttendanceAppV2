using System.ComponentModel.DataAnnotations;
using Domain.Common;

namespace Domain.Entities.Attendance;

public sealed class AttendanceLog : AuditableEntity<Guid>
{
    public DateTime DateTimeAttend { get; set; }
    public string CardNo { get; set; } = string.Empty;
    public string EmpID { get; set; } = string.Empty;
    public DateOnly DateWork { get; set; }
    public TimeSpan? TimeAttend { get; set; }  // Assuming this stores only time

    #region Device Information (للبصمة)
    public int Direct { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceNo { get; set; } = string.Empty;
    #endregion
}

public enum DirectType
{
    [Display(Name = "دخول")]
    In = 1,
    [Display(Name = "انصراف")]
    Out = 2,
}
