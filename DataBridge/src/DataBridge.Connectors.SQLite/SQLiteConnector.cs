using System.Reflection;
using System.Text;
using Dapper;
using DataBridge.Core.Interfaces;
using DataBridge.Core.Models;
using DataBridge.Core.Query;
using Microsoft.Data.Sqlite;

namespace DataBridge.Connectors.SQLite;

/// <summary>
/// Generic SQLite connector. Generates DDL and DML via reflection.
/// All filtering is done in-memory (after a full load) for simplicity and
/// cross-field correctness; SQL-level paging is applied for large datasets.
/// </summary>
public class SQLiteConnector<T> : IDataConnector<T>, IDataConnector
    where T : class, IEntity, new()
{
    private readonly string _connectionString;
    private readonly string _tableName;
    private SqliteTransaction? _activeTransaction;
    private SqliteConnection? _transactionConnection;

    public string EntityName { get; }

    public SQLiteConnector(string connectionString, string? tableName = null)
    {
        _connectionString = connectionString;
        _tableName = tableName ?? typeof(T).Name + "s";
        EntityName = typeof(T).Name;
        EnsureTableExistsAsync().GetAwaiter().GetResult();
    }

    // ─── IReadConnector ───────────────────────────────────────────────────────

    public async Task<QueryResult<T>> QueryAsync(QuerySpec spec, CancellationToken ct = default)
    {
        using var conn = OpenConnection();
        var all = (await conn.QueryAsync<T>($"SELECT * FROM {_tableName}")).ToList();

        var filtered = FilterEvaluator.Apply(all, spec.Filter).ToList();
        var sorted   = SortingEngine.Apply(filtered, spec.OrderBy).ToList();

        int from   = spec.Page?.From ?? 0;
        int offset = spec.Page?.Offset ?? sorted.Count;

        var paged = sorted.Skip(from).Take(offset).ToList();

        return new QueryResult<T>
        {
            Items      = paged,
            TotalCount = sorted.Count,
            From       = from,
            Offset     = offset
        };
    }

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var conn = OpenConnection();
        return await conn.QueryFirstOrDefaultAsync<T>(
            $"SELECT * FROM {_tableName} WHERE Id = @Id", new { Id = id.ToString() });
    }

    public async Task<IReadOnlyList<T>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var idList = ids.Select(g => g.ToString()).ToList();
        if (!idList.Any()) return Array.Empty<T>();

        using var conn = OpenConnection();
        var results = await conn.QueryAsync<T>(
            $"SELECT * FROM {_tableName} WHERE Id IN @Ids", new { Ids = idList });
        return results.ToList();
    }

    // ─── IWriteConnector ──────────────────────────────────────────────────────

    public async Task<T> InsertAsync(T entity, CancellationToken ct = default)
    {
        if (entity.Id == Guid.Empty) entity.Id = Guid.NewGuid();

        var props  = GetPersistableProperties();
        var cols   = string.Join(", ", props.Select(p => p.Name));
        var vals   = string.Join(", ", props.Select(p => "@" + p.Name));
        var sql    = $"INSERT INTO {_tableName} ({cols}) VALUES ({vals})";
        var param  = BuildDapperParam(entity, props);

        var conn = GetConnection();
        await conn.ExecuteAsync(sql, param, _activeTransaction);
        return entity;
    }

    public async Task<T> UpdateAsync(T entity, CancellationToken ct = default)
    {
        var props  = GetPersistableProperties().Where(p => p.Name != "Id").ToList();
        var sets   = string.Join(", ", props.Select(p => $"{p.Name} = @{p.Name}"));
        var sql    = $"UPDATE {_tableName} SET {sets} WHERE Id = @Id";
        var param  = BuildDapperParam(entity, GetPersistableProperties());

        var conn = GetConnection();
        await conn.ExecuteAsync(sql, param, _activeTransaction);
        return entity;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var conn   = GetConnection();
        var rows   = await conn.ExecuteAsync(
            $"DELETE FROM {_tableName} WHERE Id = @Id",
            new { Id = id.ToString() }, _activeTransaction);
        return rows > 0;
    }

    // ─── ITransactionParticipant ──────────────────────────────────────────────

    public Task BeginTransactionAsync(CancellationToken ct = default)
    {
        _transactionConnection = new SqliteConnection(_connectionString);
        _transactionConnection.Open();
        _activeTransaction = _transactionConnection.BeginTransaction();
        return Task.CompletedTask;
    }

    public Task CommitAsync(CancellationToken ct = default)
    {
        _activeTransaction?.Commit();
        CleanupTransaction();
        return Task.CompletedTask;
    }

    public Task RollbackAsync(CancellationToken ct = default)
    {
        _activeTransaction?.Rollback();
        CleanupTransaction();
        return Task.CompletedTask;
    }

    // ─── IDataConnector (non-generic) ────────────────────────────────────────

    async Task<IEntity?> IDataConnector.GetByIdAsync(Guid id, CancellationToken ct)
        => await GetByIdAsync(id, ct);

    async Task<IReadOnlyList<IEntity>> IDataConnector.GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct)
        => (await GetByIdsAsync(ids, ct)).Cast<IEntity>().ToList();

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private SqliteConnection OpenConnection()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }

    private SqliteConnection GetConnection()
    {
        if (_transactionConnection != null) return _transactionConnection;
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }

    private void CleanupTransaction()
    {
        _activeTransaction?.Dispose();
        _activeTransaction = null;
        _transactionConnection?.Dispose();
        _transactionConnection = null;
    }

    private static List<PropertyInfo> GetPersistableProperties()
        => typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite && IsScalar(p.PropertyType))
            .ToList();

    private static bool IsScalar(Type t)
    {
        var u = Nullable.GetUnderlyingType(t) ?? t;
        return u.IsPrimitive || u == typeof(string) || u == typeof(decimal)
            || u == typeof(DateTime) || u == typeof(Guid);
    }

    private static object BuildDapperParam(T entity, IEnumerable<PropertyInfo> props)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var p in props)
        {
            var val = p.GetValue(entity);
            // Dapper needs Guid/DateTime as strings for SQLite
            dict[p.Name] = val switch
            {
                Guid g      => g.ToString(),
                DateTime dt => dt.ToString("O"),
                _           => val
            };
        }
        return dict;
    }

    private async Task EnsureTableExistsAsync()
    {
        var props = GetPersistableProperties();
        var cols  = new StringBuilder();
        foreach (var p in props)
        {
            var sqlType = GetSqliteType(p.PropertyType);
            var pk      = p.Name == "Id" ? " PRIMARY KEY" : "";
            cols.Append($"  {p.Name} {sqlType}{pk},\n");
        }
        if (cols.Length > 0) cols.Length -= 2; // remove last comma+newline

        var ddl = $"CREATE TABLE IF NOT EXISTS {_tableName} (\n{cols}\n);";
        using var conn = OpenConnection();
        await conn.ExecuteAsync(ddl);
    }

    private static string GetSqliteType(Type t)
    {
        var u = Nullable.GetUnderlyingType(t) ?? t;
        if (u == typeof(int) || u == typeof(long) || u == typeof(bool)) return "INTEGER";
        if (u == typeof(double) || u == typeof(float) || u == typeof(decimal)) return "REAL";
        return "TEXT";
    }
}
