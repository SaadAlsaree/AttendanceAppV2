using System.ComponentModel.DataAnnotations;

namespace Domain.Enums;



public enum ScheduleType
{
    [Display(Name = "منتظم")]
    Regular = 1,
    [Display(Name = "متناوب")]
    Rotating = 2,
    [Display(Name = "مرن")]
    Flexible = 3,
    [Display(Name = "مخصص")]
    Custom = 4,
}
