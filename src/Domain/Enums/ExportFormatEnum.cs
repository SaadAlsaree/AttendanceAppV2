using System.ComponentModel.DataAnnotations;

namespace Domain.Enums;

public enum ExportFormat
{
    [Display(Name = "لا يوجد")]
    None = 0,
    [Display(Name = "PDF")]
    Pdf = 1,
    [Display(Name = "Excel")]
    Excel = 2,
    [Display(Name = "CSV")]
    Csv = 3,
    [Display(Name = "JSON")]
    Json = 4
}
