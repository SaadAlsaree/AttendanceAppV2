using System.ComponentModel.DataAnnotations;

namespace Domain.Enums;

public enum DeviceStatus
{
    [Display(Name = "متصل")]
    Online,
    [Display(Name = "غير متصل")]
    Offline,
    [Display(Name = "صيانة")]
    Maintenance
}
