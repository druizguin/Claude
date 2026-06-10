using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using DataBridge.Core.Interfaces;
using DataBridge.Core.Models;
using DataBridge.Core.Query;

namespace DataBridge.Connectors.CSV;

/// <summary>
/// Generic CSV connector. Loads the full file into memory; writes flush to disk.
/// Supports distributed transactions via copy-on-write staging.
/// </summary>
public class CsvConnector<T> : IDataConnector<T>, IDataConnector
    where T : class, IEntity, new()
{
    private readonly string _filePath;
    private List<T> _snapshot = new();
    private List<T>? _stagingList;

    public string EntityName { get; }

    public CsvConnector(string filePath)
    {
        _filePath  = filePath;
        EntityName = typeof(T).Name;
    }

    // ─── IReadConnector ───────────────────────────────────────────────────────

    public Task<QueryResult<T>> QueryAsync(QuerySpec spec, CancellationToken ct = default)
    {
        var source   = CurrentList();
        var filtered = FilterEvaluator.Apply(source, spec.Filter).ToList();
        var sorted   = SortingEngine.Apply(filtered, spec.OrderBy).ToList();

        int from   = spec.Page?.From ?? 0;
        int offset = spec.Page?.Offset ?? sorted.Count;
        var paged  = sorted.Skip(from).Take(offset).ToList();

        return Task.FromResult(new QueryResult<T>
        {
            Items      = paged,
            TotalCount = sorted.Count,
            From       = from,
            Offset     = offset
        });
    }

    public Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(CurrentList().FirstOrDefault(e => e.Id == id));

    public Task<IReadOnlyList<T>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var set    = ids.ToHashSet();
        var result = CurrentList().Where(e => set.Contains(e.Id)).ToList();
        return Task.FromResult<IReadOnlyList<T>>(result);
    }

    // ─── IWriteConnector ──────────────────────────────────────────────────────

    public Task<T> InsertAsync(T entity, CancellationToken ct = default)
    {
        if (entity.Id == Guid.Empty) entity.Id = Guid.NewGuid();
        WorkingList().Add(entity);
        if (_stagingList == null) FlushToDisk();
        return Task.FromResult(entity);
    }

    public Task<T> UpdateAsync(T entity, CancellationToken ct = default)
    {
        var list  = WorkingList();
        var index = list.FindIndex(e => e.Id == entity.Id);
        if (index >= 0) list[index] = entity;
        if (_stagingList == null) FlushToDisk();
        return Task.FromResult(entity);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var list    = WorkingList();
        var removed = list.RemoveAll(e => e.Id == id) > 0;
        if (removed && _stagingList == null) FlushToDisk();
        return Task.FromResult(removed);
    }

    // ─── ITransactionParticipant ──────────────────────────────────────────────

    public Task BeginTransactionAsync(CancellationToken ct = default)
    {
        _snapshot    = new List<T>(CurrentList());
        _stagingList = new List<T>(_snapshot);
        return Task.CompletedTask;
    }

    public Task CommitAsync(CancellationToken ct = default)
    {
        if (_stagingList != null)
        {
            _snapshot    = _stagingList;
            _stagingList = null;
            FlushToDisk();
        }
        return Task.CompletedTask;
    }

    public Task RollbackAsync(CancellationToken ct = default)
    {
        _stagingList = null;
        return Task.CompletedTask;
    }

    // ─── IDataConnector (non-generic) ────────────────────────────────────────

    async Task<IEntity?> IDataConnector.GetByIdAsync(Guid id, CancellationToken ct)
        => await GetByIdAsync(id, ct);

    async Task<IReadOnlyList<IEntity>> IDataConnector.GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct)
        => (await GetByIdsAsync(ids, ct)).Cast<IEntity>().ToList();

    // ─── Disk I/O ─────────────────────────────────────────────────────────────

    private List<T> CurrentList()
    {
        if (_stagingList != null) return _stagingList;
        if (_snapshot.Count == 0 && File.Exists(_filePath))
            _snapshot = LoadFromDisk();
        return _snapshot;
    }

    private List<T> WorkingList()
    {
        if (_stagingList != null) return _stagingList;
        if (_snapshot.Count == 0 && File.Exists(_filePath))
            _snapshot = LoadFromDisk();
        return _snapshot;
    }

    private List<T> LoadFromDisk()
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true };
        using var reader = new StreamReader(_filePath);
        using var csv    = new CsvReader(reader, config);
        return csv.GetRecords<T>().ToList();
    }

    public void FlushToDisk()
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true };
        using var writer = new StreamWriter(_filePath, append: false);
        using var csv    = new CsvWriter(writer, config);
        csv.WriteRecords(_stagingList ?? _snapshot);
    }
}
