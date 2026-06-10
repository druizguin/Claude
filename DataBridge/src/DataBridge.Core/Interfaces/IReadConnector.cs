using DataBridge.Core.Models;

namespace DataBridge.Core.Interfaces;

public interface IReadConnector<T> where T : class, IEntity
{
    Task<QueryResult<T>> QueryAsync(QuerySpec spec, CancellationToken ct = default);
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
}
