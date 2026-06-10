using DataBridge.Core.Models;

namespace DataBridge.Core.Interfaces;

public interface IAuditService
{
    Task LogAsync(AuditRecord record, CancellationToken ct = default);
    Task<QueryResult<AuditRecord>> QueryAsync(QuerySpec spec, CancellationToken ct = default);
    Task<IReadOnlyList<AuditRecord>> GetByEntityIdAsync(Guid entityId, CancellationToken ct = default);
}
