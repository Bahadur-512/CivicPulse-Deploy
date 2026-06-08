namespace CivicPulse.Core.Entities;

public class AuditLog : BaseEntity
{
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public string OldValues { get; set; } = string.Empty;
    public string NewValues { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public int? UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public string Description { get; set; } = string.Empty;
}
