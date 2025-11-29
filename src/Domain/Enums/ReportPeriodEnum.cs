using System.ComponentModel.DataAnnotations;

namespace Domain.Enums;




public enum ReportPeriod
{
    [Display(Name = "اليوم")]
    Today = 1,

    [Display(Name = "اليوم السابق")]
    Yesterday = 2,
    [Display(Name = "هذا الأسبوع")]
    This_Week = 3,
    [Display(Name = "الأسبوع الماضي")]
    Last_Week = 4,
    [Display(Name = "هذا الشهر")]
    This_Month = 5,
    [Display(Name = "الشهر الماضي")]
    Last_Month = 6,
    [Display(Name = "هذا العام")]
    This_Year = 7,
    [Display(Name = "المدة المخصصة")]
    Custom_Range = 8,
}
