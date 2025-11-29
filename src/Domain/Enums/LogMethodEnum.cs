using System.ComponentModel.DataAnnotations;

namespace Domain.Enums;



public enum LogMethod
{
    [Display(Name = "موبايل")]
    Mobile_App = 1,
    [Display(Name = "ويب")]
    Web = 2,
    [Display(Name = "بيومتريك")]
    Biometric = 3,
    [Display(Name = "بطاقة RFID")]
    RFID_Card = 4,
    [Display(Name = "بطاقة NFC")]
    NFC_Card = 5,
    [Display(Name = "بطاقة QR")]
    QR_Card = 6,
    [Display(Name = "إدخال يدوي")]
    Manual_Entry = 7,
    [Display(Name = "API")]
    API = 8,
}
