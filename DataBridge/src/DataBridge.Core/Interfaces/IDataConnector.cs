using DataBridge.Core.Models;

namespace DataBridge.Core.Interfaces;

public interface IDataConnector<T> : IReadConnector<T>, IWriteConnector<T>, ITransactionParticipant
    where T : class, IEntity
{
    string EntityName { get; }
}

/// <summary>Non-generic marker interface for registry lookups.</summary>
public interface IDataConnector
{
    string EntityName { get; }
    Task<IEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<IEntity>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
}
