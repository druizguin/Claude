using Audit.Dom.Enums;

namespace Audit.Dom.Entities;

public class AuditEntry
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public AuditAction Action { get; set; }
    public DateTime Timestamp { get; set; }
    public List<AuditDetail> Details { get; set; } = [];
}
