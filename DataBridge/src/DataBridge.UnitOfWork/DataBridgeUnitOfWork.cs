using DataBridge.Core.Interfaces;
using DataBridge.Core.Models;

namespace DataBridge.UnitOfWork;

/// <summary>
/// Orchestrates queries and writes across multiple data sources.
/// Provides:
///   - Generic CRUD routed to the registered connector for each entity type
///   - Cross-source navigation-property resolution (parallel batch loading)
///   - Distributed transactions via DistributedTransactionCoordinator
///   - Audit logging delegated to IAuditService
/// </summary>
public class DataBridgeUnitOfWork : IUnitOfWork
{
    private readonly EntitySourceRegistry _registry;
    private readonly CrossSourceResolver _resolver;
    private readonly IAuditService _audit;
    private DistributedTransactionCoordinator? _transaction;

    public DataBridgeUnitOfWork(
        EntitySourceRegistry registry,
        IAuditService audit)
    {
        _registry = registry;
        _resolver = new CrossSourceResolver(registry);
        _audit    = audit;
    }

    // ─── Query ────────────────────────────────────────────────────────────────

    public async Task<QueryResult<T>> QueryAsync<T>(QuerySpec spec, CancellationToken ct = default)
        where T : class, IEntity
    {
        var connector = GetConnector<T>();
        var result    = await connector.QueryAsync(spec, ct);

        // Resolve cross-source navigation properties in parallel
        await _resolver.ResolveAsync(result.Items, ct);

        await LogAudit(OperationType.Read, Guid.Empty, typeof(T).Name, "system", ct);

        return result;
    }

    public async Task<T?> GetByIdAsync<T>(Guid id, CancellationToken ct = default)
        where T : class, IEntity
    {
        var connector = GetConnector<T>();
        var entity    = await connector.GetByIdAsync(id, ct);

        if (entity != null)
        {
            await _resolver.ResolveAsync(new List<T> { entity }, ct);
            await LogAudit(OperationType.Read, id, typeof(T).Name, "system", ct);
        }

        return entity;
    }

    // ─── Write ────────────────────────────────────────────────────────────────

    public async Task<T> InsertAsync<T>(T entity, string personName, CancellationToken ct = default)
        where T : class, IEntity
    {
        var connector = GetConnector<T>();
        var result    = await connector.InsertAsync(entity, ct);
        await LogAudit(OperationType.Create, result.Id, typeof(T).Name, personName, ct);
        return result;
    }

    public async Task<T> UpdateAsync<T>(T entity, string personName, CancellationToken ct = default)
        where T : class, IEntity
    {
        var connector = GetConnector<T>();
        var result    = await connector.UpdateAsync(entity, ct);
        await LogAudit(OperationType.Update, result.Id, typeof(T).Name, personName, ct);
        return result;
    }

    public async Task<bool> DeleteAsync<T>(Guid id, string personName, CancellationToken ct = default)
        where T : class, IEntity
    {
        var connector = GetConnector<T>();
        var deleted   = await connector.DeleteAsync(id, ct);
        if (deleted)
            await LogAudit(OperationType.Delete, id, typeof(T).Name, personName, ct);
        return deleted;
    }

    // ─── Distributed Transaction ──────────────────────────────────────────────

    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        var participants = _registry.AllConnectors()
            .OfType<ITransactionParticipant>()
            .ToList();

        _transaction = new DistributedTransactionCoordinator(participants);
        await _transaction.BeginAsync(ct);
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (_transaction == null) return;
        await _transaction.CommitAsync(ct);
        _transaction = null;
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (_transaction == null) return;
        await _transaction.RollbackAsync(ct);
        _transaction = null;
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private IDataConnector<T> GetConnector<T>() where T : class, IEntity
        => _registry.GetConnector<T>()
           ?? throw new InvalidOperationException(
               $"No connector registered for entity type '{typeof(T).Name}'.");

    private Task LogAudit(OperationType op, Guid entityId, string entityType, string person, CancellationToken ct)
        => _audit.LogAsync(new AuditRecord
        {
            Id            = Guid.NewGuid(),
            Timestamp     = DateTime.UtcNow,
            OperationType = op,
            EntityId      = entityId,
            EntityType    = entityType,
            PersonName    = person
        }, ct);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
