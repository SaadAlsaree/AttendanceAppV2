using SharedKernel;

namespace Domain.Common;

public class AuditableEntity<T> : Entity
{
    public T Id { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
    public Guid? LastUpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime? DoneProcdureDate { get; set; }
    public Guid? DeletedBy { get; set; }
}
