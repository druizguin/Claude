using DataBridge.Core.Models;

namespace DataBridge.Core.Interfaces;

public interface IWriteConnector<T> where T : class, IEntity
{
    Task<T> InsertAsync(T entity, CancellationToken ct = default);
    Task<T> UpdateAsync(T entity, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
