namespace Audit.Dom.Entities;

public class AuditDetail
{
    public Guid Id { get; set; }
    public Guid AuditId { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
}
