using System.ComponentModel.DataAnnotations;

namespace Domain.Enums;



public enum LogType
{
    [Display(Name = "حضور")]
    Check_In = 1,
    [Display(Name = "انصراف")]
    Check_Out = 2,

}
