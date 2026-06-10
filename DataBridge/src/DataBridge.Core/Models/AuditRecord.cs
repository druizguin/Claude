namespace DataBridge.Core.Models;

public class AuditRecord : IEntity
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public OperationType OperationType { get; set; }
    public Guid EntityId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string PersonName { get; set; } = string.Empty;
}
