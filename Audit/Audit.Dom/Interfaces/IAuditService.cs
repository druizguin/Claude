using Audit.Dom.Entities;

namespace Audit.Dom.Interfaces;

public interface IAuditService
{
    Task<Guid> CreateAuditAsync(AuditEntry entry, CancellationToken ct = default);
    Task<AuditEntry?> GetAuditByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<AuditEntry>> GetAuditsByUserIdAsync(string userId, int skip = 0, int take = 50, CancellationToken ct = default);
    Task<IEnumerable<AuditEntry>> GetAllAuditsAsync(int skip = 0, int take = 50, CancellationToken ct = default);
    Task<int> GetTotalCountAsync(CancellationToken ct = default);
    Task<int> GetCountByUserIdAsync(string userId, CancellationToken ct = default);
}
