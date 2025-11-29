using Domain.Common;
using Domain.Entities.Organizations;
using Domain.Enums;

namespace Domain.Entities.Attendance;
public class AttendanceException : AuditableEntity<Guid>
{
    public Guid AttendanceId { get; set; }

    /// <summary>
    /// نوع الاستثناء/التعديل
    /// مثال: تصحيح وقت الدخول، تصحيح وقت الخروج، إضافة حضور يدوي، حذف تأخير
    /// </summary>
    public ExceptionType ExceptionType { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    /// <summary>
    /// سبب التعديل
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    public string? Notes { get; set; }


    public Guid ModifiedBy { get; set; }

    /// <summary>
    /// معرف المدير الذي وافق على التعديل (إن لزم)
    /// </summary>
    public Guid? ApprovedBy { get; set; }


    public DateTime? ApprovedAt { get; set; }

    /// <summary>
    /// هل التعديل موافق عليه؟
    /// </summary>
    public bool? IsApproved { get; set; } = false;

    public int? Priority { get; set; } = 0;

    public Guid? ModifierId { get; set; }
    public Guid? ApproverId { get; set; }

    #region Navigation Properties

    public virtual Attendance Attendance { get; set; } = null!;

    public virtual Employee Modifier { get; set; } = null!;

    public virtual Employee? Approver { get; set; }
    #endregion
}
