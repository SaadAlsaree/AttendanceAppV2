using System.ComponentModel.DataAnnotations;

namespace Domain.Enums;


public enum LogStatus
{
    [Display(Name = "قيد المراجعة")]
    Pending = 1,
    [Display(Name = "موثوق")]
    Verified = 2,
    [Display(Name = "مرفوض")]
    Rejected = 3,
    [Display(Name = "موافق عليه")]
    Approved = 4,
    [Display(Name = "ملغي")]
    Cancelled = 5,
    [Display(Name = "قيد الموافقة")]
    Pending_Approval = 6,
    [Display(Name = "قيد التحقق")]
    Pending_Verification = 7,
    [Display(Name = "قيد الرفض")]
    Pending_Rejection = 8

}
