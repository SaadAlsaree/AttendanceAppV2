using System.ComponentModel.DataAnnotations;

namespace Domain.Enums;



public enum ReportType
{
    [Display(Name = "يومي")]
    Daily = 1,
    [Display(Name = "أسبوعي")]
    Weekly = 2,
    [Display(Name = "شهري")]
    Monthly = 3,
    [Display(Name = "سنوي")]
    Yearly = 4,
    [Display(Name = "مخصص")]
    Custom = 5,

    [Display(Name = "ملخص الموظف")]
    Employee_Summary = 6,
    [Display(Name = "ملخص جهة")]
    Unit_Summary = 7,
    [Display(Name = "تأخير الوصول")]
    Late_Arrivals = 8,
    [Display(Name = "الانصراف المبكر")]
    Early_Unit = 9,

    [Display(Name = "عمل إضافي")]
    Overtime = 10,
    [Display(Name = "غياب")]
    Absences = 11,
    [Display(Name = "إجازات")]
    Vacations = 12,


}
