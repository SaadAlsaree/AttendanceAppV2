using System.ComponentModel.DataAnnotations;

namespace Domain.Enums;

public enum LeaveStatus
{
    [Display(Name = "قيد الانتظار")]
    Pending = 1,
    [Display(Name = "موافق عليها")]
    Approved = 2,
    [Display(Name = "مرفوضة")]
    Rejected = 3,
    [Display(Name = "ملغاة")]
    Cancelled = 4,
    [Display(Name = "منتهية")]
    Expired = 5,
    [Display(Name = "قيد المراجعة")]
    UnderReview = 6
}
