using System.ComponentModel.DataAnnotations;

namespace Domain.Enums;


public enum AttendanceStatus
{
    [Display(Name = "حضور")]
    Present = 1,
    [Display(Name = "غياب")]
    Absent = 2,
    [Display(Name = "راحة")]
    Break = 3,
    [Display(Name = "إجازة")]
    Vacation = 4,
    [Display(Name = "عطلة")]
    Holiday = 5,
    [Display(Name = "تأخير")]
    Late = 6,
    [Display(Name = "انصراف مبكر")]
    Early_Out = 7,
    [Display(Name = "عمل إضافي")]
    Overtime = 8,
    [Display(Name = "واجب")]
    Duty = 9,
    [Display(Name = "مستثنى")]
    Exempted = 10,
    [Display(Name = "منسب")]
    Permitted = 11,
    [Display(Name = "قيد المعالجة")]
    Pending = 12,
}
