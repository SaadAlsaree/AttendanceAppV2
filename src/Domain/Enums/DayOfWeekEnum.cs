using System.ComponentModel.DataAnnotations;

namespace Domain.Enums;
public enum DayOfWeek
{
    [Display(Name = "الاحد")]
    Sunday = 1,
    [Display(Name = "الاثنين")]
    Monday = 2,
    [Display(Name = "الثلاثاء")]
    Tuesday = 3,
    [Display(Name = "الأربعاء")]
    Wednesday = 4,
    [Display(Name = "الخميس")]
    Thursday = 5,
    [Display(Name = "الجمعة")]
    Friday = 6,
    [Display(Name = "السبت")]
    Saturday = 7,
}
