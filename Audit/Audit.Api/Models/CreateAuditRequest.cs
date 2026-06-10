using Audit.Dom.Enums;

namespace Audit.Api.Models;

public class CreateAuditRequest
{
    public string UserId { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public AuditAction Action { get; set; }
    public List<CreateAuditDetailRequest> Details { get; set; } = [];
}

public class CreateAuditDetailRequest
{
    public string PropertyName { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
}
