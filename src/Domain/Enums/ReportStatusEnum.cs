using System.ComponentModel.DataAnnotations;

namespace Domain.Enums;



public enum ReportStatus
{
    [Display(Name = "قيد المعالجة")]
    Pending = 1,
    [Display(Name = "قيد التوليد")]
    Generating = 2,
    [Display(Name = "تم التوليد")]
    Generated = 3,
    [Display(Name = "فشل التوليد")]
    Failed = 4,
}
