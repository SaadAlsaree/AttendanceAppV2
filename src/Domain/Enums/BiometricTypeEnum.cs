using System.ComponentModel.DataAnnotations;

namespace Domain.Enums;

public enum BiometricType
{
    [Display(Name = "بصمة الإصبع")]
    Fingerprint = 1,
    [Display(Name = "بصمة الوجه")]
    Face = 2,
    [Display(Name = "بصمة العين")]
    Iris = 3,
    [Display(Name = "بصمة الصوت")]
    Voice = 4,
    [Display(Name = "بصمة كف اليد")]
    Palm = 5
}
