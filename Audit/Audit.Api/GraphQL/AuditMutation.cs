using Audit.Dom.Entities;
using Audit.Dom.Enums;
using Audit.Dom.Interfaces;

namespace Audit.Api.GraphQL;

public class AuditMutation
{
    public async Task<AuditEntry> CreateAudit(
        [Service] IAuditService service,
        string userId,
        Guid entityId,
        string entityName,
        AuditAction action,
        List<AuditDetailInput>? details = null,
        CancellationToken ct = default)
    {
        var entry = new AuditEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EntityId = entityId,
            EntityName = entityName,
            Action = action,
            Timestamp = DateTime.UtcNow,
            Details = details?.Select(d => new AuditDetail
            {
                Id = Guid.NewGuid(),
                PropertyName = d.PropertyName,
                OldValue = d.OldValue,
                NewValue = d.NewValue
            }).ToList() ?? []
        };

        var id = await service.CreateAuditAsync(entry, ct);
        return (await service.GetAuditByIdAsync(id, ct))!;
    }
}

public record AuditDetailInput(string PropertyName, string? OldValue, string? NewValue);
