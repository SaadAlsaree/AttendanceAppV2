using System.ComponentModel.DataAnnotations;

namespace Domain.Enums;




public enum ExceptionType
{
    [Display(Name = "عطلة")]
    Holiday = 1,
    [Display(Name = "عمل إضافي")]
    Overtime = 2,
    [Display(Name = "شفت مختلف")]
    Different_Shift = 3,
    [Display(Name = "لا يوجد عمل")]
    No_Work = 4,
    [Display(Name = "واجب")]
    Duty
}
