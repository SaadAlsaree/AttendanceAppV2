using System.ComponentModel.DataAnnotations;

namespace Domain.Enums;



public enum ShiftType

{
    [Display(Name = "صباحي")]
    Morning = 1,
    [Display(Name = "مسائي")]
    Afternoon = 2,
    [Display(Name = "بعد الظهر")]
    Evening = 3,
    [Display(Name = "خفر")]
    Night = 4,
    [Display(Name = "مرن")]
    Flexible = 5,
    [Display(Name = "مخصص")]
    Custom = 6,
}
