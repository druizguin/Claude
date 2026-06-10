using Audit.Dom.Entities;
using Audit.Dom.Interfaces;

namespace Audit.Api.GraphQL;

public class AuditQuery
{
    public async Task<IEnumerable<AuditEntry>> GetAudits(
        [Service] IAuditService service,
        int skip = 0,
        int take = 50,
        CancellationToken ct = default)
        => await service.GetAllAuditsAsync(skip, take, ct);

    public async Task<AuditEntry?> GetAuditById(
        [Service] IAuditService service,
        Guid id,
        CancellationToken ct = default)
        => await service.GetAuditByIdAsync(id, ct);

    public async Task<IEnumerable<AuditEntry>> GetAuditsByUser(
        [Service] IAuditService service,
        string userId,
        int skip = 0,
        int take = 50,
        CancellationToken ct = default)
        => await service.GetAuditsByUserIdAsync(userId, skip, take, ct);

    public async Task<int> GetAuditCount(
        [Service] IAuditService service,
        CancellationToken ct = default)
        => await service.GetTotalCountAsync(ct);
}
