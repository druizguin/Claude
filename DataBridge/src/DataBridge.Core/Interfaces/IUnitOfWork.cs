using DataBridge.Core.Models;

namespace DataBridge.Core.Interfaces;

public interface IUnitOfWork : IAsyncDisposable
{
    Task<QueryResult<T>> QueryAsync<T>(QuerySpec spec, CancellationToken ct = default)
        where T : class, IEntity;

    Task<T?> GetByIdAsync<T>(Guid id, CancellationToken ct = default)
        where T : class, IEntity;

    Task<T> InsertAsync<T>(T entity, string personName, CancellationToken ct = default)
        where T : class, IEntity;

    Task<T> UpdateAsync<T>(T entity, string personName, CancellationToken ct = default)
        where T : class, IEntity;

    Task<bool> DeleteAsync<T>(Guid id, string personName, CancellationToken ct = default)
        where T : class, IEntity;

    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}
