using System.ComponentModel.DataAnnotations;

namespace Domain.Enums;

public enum LocationVerificationStatus
{
    [Display(Name = "موافق")]
    Verified = 1,
    [Display(Name = "خارج النطاق")]
    OutOfRange = 2,
    [Display(Name = "غير محدد")]
    Unknown = 3,
    [Display(Name = "تحقق يدوي")]
    ManualVerification = 4,
    [Display(Name = "خطأ في الموقع")]
    LocationError = 5
}
