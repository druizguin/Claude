using Audit.Dom.Entities;
using Audit.Dom.Interfaces;

namespace Audit.Svc;

public class AuditService : IAuditService
{
    private readonly IAuditRepository _repository;

    public AuditService(IAuditRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> CreateAuditAsync(AuditEntry entry, CancellationToken ct = default)
    {
        if (entry.Id == Guid.Empty)
            entry.Id = Guid.NewGuid();

        if (entry.Timestamp == default)
            entry.Timestamp = DateTime.UtcNow;

        foreach (var detail in entry.Details.Where(d => d.Id == Guid.Empty))
        {
            detail.Id = Guid.NewGuid();
            detail.AuditId = entry.Id;
        }

        return await _repository.CreateAsync(entry, ct);
    }

    public Task<AuditEntry?> GetAuditByIdAsync(Guid id, CancellationToken ct = default)
        => _repository.GetByIdAsync(id, ct);

    public Task<IEnumerable<AuditEntry>> GetAuditsByUserIdAsync(string userId, int skip = 0, int take = 50, CancellationToken ct = default)
        => _repository.GetByUserIdAsync(userId, skip, take, ct);

    public Task<IEnumerable<AuditEntry>> GetAllAuditsAsync(int skip = 0, int take = 50, CancellationToken ct = default)
        => _repository.GetAllAsync(skip, take, ct);

    public Task<int> GetTotalCountAsync(CancellationToken ct = default)
        => _repository.CountAllAsync(ct);

    public Task<int> GetCountByUserIdAsync(string userId, CancellationToken ct = default)
        => _repository.CountByUserIdAsync(userId, ct);
}
