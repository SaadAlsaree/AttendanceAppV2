using System.ComponentModel.DataAnnotations;

namespace Domain.Enums;

public enum LeaveType
{
    [Display(Name = "إجازة أعتيادية")]
    Ordinary = 1,
    [Display(Name = "إجازة مرضية")]
    Sick = 2,
    [Display(Name = "إجازة طارئة")]
    Emergency = 3,
    [Display(Name = "إجازة أمومة")]
    Maternity = 4,
    [Display(Name = "إجازة زمنية")]
    TimeOff = 5,
    [Display(Name = "إجازة حج")]
    Hajj = 6,
    [Display(Name = "إجازة عمرة")]
    Umrah = 7,
    [Display(Name = "إجازة دراسية")]
    Study = 8,
    [Display(Name = "إجازة بدون راتب")]
    Unpaid = 9,
    [Display(Name = "إجازة تعويضية")]
    Compensatory = 10,
    [Display(Name = "واجب")]
    Duty = 11,
    [Display(Name = "استراحة خفر")]
    Night_Break = 12,
    [Display(Name = "تنسيب")]
    Permitted = 13,
    [Display(Name = "دورة")]
    Cycle = 14,
    [Display(Name = "ورشة عمل")]
    Workshop = 15,
}
