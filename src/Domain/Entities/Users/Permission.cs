using System.ComponentModel.DataAnnotations.Schema;
using Domain.Common;
using NpgsqlTypes;

namespace Domain.Entities.Users;

public class Permission : AuditableEntity<Guid>
{

    [Column(TypeName = "varchar(100)")]
    public string Name { get; set; }

    [Column(TypeName = "varchar(255)")]
    public string Description { get; set; }

    [Column(TypeName = "varchar(50)")]
    public string Resource { get; set; }

    [Column(TypeName = "varchar(50)")]
    public string Action { get; set; }

    public bool IsActive { get; set; } = true;

    [Column(TypeName = "jsonb")]
    [PgName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();

    // Navigation property for many-to-many relationship with User through UserPermission
    public ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
}
