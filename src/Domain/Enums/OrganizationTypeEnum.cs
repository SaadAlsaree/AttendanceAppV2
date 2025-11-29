using System.ComponentModel.DataAnnotations;

namespace Domain.Enums;



public enum OrganizationType
{
    [Display(Name = "دائرة")]
    Directorate = 1,
    [Display(Name = "مديرية")]
    Department = 2,
    [Display(Name = "قسم")]
    Section = 3,
    [Display(Name = "مكتب")]
    Office = 4,

}
