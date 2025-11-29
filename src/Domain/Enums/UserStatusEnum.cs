using System.ComponentModel.DataAnnotations;

namespace Domain.Enums;


public enum UserStatus
{
    [Display(Name = "مفعل")]
    Active = 1,
    [Display(Name = "غير مفعل")]
    Inactive = 2,
    [Display(Name = "قيد التحقق")]
    Pending = 3,
    [Display(Name = "مقفل")]
    Locked = 4,
    [Display(Name = "منتهي الصلاحية")]
    Expired = 5,
    [Display(Name = "محذوف")]
    Deleted = 6,
    [Display(Name = "معلق")]
    Suspended = 7,
    [Display(Name = "مؤرشف")]
    Archived = 8,
}
