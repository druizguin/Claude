using Audit.Dom.Entities;

namespace Audit.Dom.Interfaces;

public interface IAuditRepository
{
    Task<Guid> CreateAsync(AuditEntry entry, CancellationToken ct = default);
    Task<AuditEntry?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<AuditEntry>> GetByUserIdAsync(string userId, int skip = 0, int take = 50, CancellationToken ct = default);
    Task<IEnumerable<AuditEntry>> GetAllAsync(int skip = 0, int take = 50, CancellationToken ct = default);
    Task<int> CountByUserIdAsync(string userId, CancellationToken ct = default);
    Task<int> CountAllAsync(CancellationToken ct = default);
}
