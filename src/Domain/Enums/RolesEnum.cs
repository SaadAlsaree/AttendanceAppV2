using System.ComponentModel.DataAnnotations;

namespace Domain.Enums;


public enum Role
{
    [Display(Name = "مسؤل")]
    Admin = 1,
    [Display(Name = "مستخدم")]
    User = 2,
    [Display(Name = "مدير")]
    Manager = 3,
    [Display(Name = "موظف")]
    Employee = 4,
    [Display(Name = "زائر")]
    Guest = 5,
    [Display(Name = "مدير الموارد البشرية")]
    HR_Manager = 6,
    [Display(Name = "مشاهد")]
    Viewer = 7,
    [Display(Name = "مدير النظام")]
    SuperAdmin = 8,
    [Display(Name = "مستخدم النظام")]
    SystemUser = 9,
    [Display(Name = "مدير النظام")]
    SystemManager = 10,
    [Display(Name = "ضابط أمن")]
    SecurityOfficer = 11,
    [Display(Name = "مشرف جهة")]
    OrgSupervisor = 12,
}
