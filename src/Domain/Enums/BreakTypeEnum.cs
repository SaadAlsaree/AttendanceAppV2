using System.ComponentModel.DataAnnotations;

namespace Domain.Enums;



public enum BreakType
{
    [Display(Name = "تعويضية")]
    Compensatory = 1,
    [Display(Name = "عطلة")]
    Holiday = 2,
    [Display(Name = "إجازة")]
    Vacation = 3,

}
