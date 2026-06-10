using System.Reflection;
using ClosedXML.Excel;
using DataBridge.Core.Interfaces;
using DataBridge.Core.Models;
using DataBridge.Core.Query;

namespace DataBridge.Connectors.Excel;

/// <summary>
/// Read-only Excel connector (xlsx). Loads the entire sheet into memory on first access.
/// Write operations stage changes to an in-memory list; Commit flushes to disk.
/// Cross-source distributed transactions are supported via the saga/snapshot approach.
/// </summary>
public class ExcelConnector<T> : IDataConnector<T>, IDataConnector
    where T : class, IEntity, new()
{
    private readonly string _filePath;
    private readonly string _worksheetName;
    private List<T> _snapshot = new();
    private List<T>? _stagingList;

    public string EntityName { get; }

    public ExcelConnector(string filePath, string? worksheetName = null)
    {
        _filePath      = filePath;
        _worksheetName = worksheetName ?? typeof(T).Name + "s";
        EntityName     = typeof(T).Name;
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
        // Snapshot current state; staging takes writes
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
        var result = new List<T>();
        using var wb = new XLWorkbook(_filePath);

        if (!wb.TryGetWorksheet(_worksheetName, out var ws))
            return result;

        var props   = GetMappableProperties();
        var headers = new Dictionary<int, PropertyInfo>();

        var headerRow = ws.Row(1);
        int lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;
        for (int col = 1; col <= lastCol; col++)
        {
            var header = headerRow.Cell(col).GetString();
            var prop   = props.FirstOrDefault(p => p.Name.Equals(header, StringComparison.OrdinalIgnoreCase));
            if (prop != null) headers[col] = prop;
        }

        int lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
        for (int row = 2; row <= lastRow; row++)
        {
            var entity = new T();
            foreach (var (col, prop) in headers)
            {
                var cell  = ws.Row(row).Cell(col);
                var value = ParseCellValue(cell, prop.PropertyType);
                prop.SetValue(entity, value);
            }
            result.Add(entity);
        }

        return result;
    }

    public void FlushToDisk()
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        using var wb = File.Exists(_filePath) ? new XLWorkbook(_filePath) : new XLWorkbook();

        if (wb.TryGetWorksheet(_worksheetName, out var existing))
            existing.Delete();

        var ws    = wb.AddWorksheet(_worksheetName);
        var props = GetMappableProperties();

        for (int i = 0; i < props.Count; i++)
            ws.Cell(1, i + 1).Value = props[i].Name;

        var list = _stagingList ?? _snapshot;
        for (int r = 0; r < list.Count; r++)
        {
            for (int c = 0; c < props.Count; c++)
            {
                var val = props[c].GetValue(list[r]);
                ws.Cell(r + 2, c + 1).Value = val?.ToString() ?? string.Empty;
            }
        }

        wb.SaveAs(_filePath);
    }

    private static List<PropertyInfo> GetMappableProperties()
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

    private static object? ParseCellValue(IXLCell cell, Type targetType)
    {
        var raw = cell.GetString();
        if (string.IsNullOrWhiteSpace(raw)) return null;

        var u = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (u == typeof(Guid))          return Guid.Parse(raw);
        if (u == typeof(int))           return int.Parse(raw);
        if (u == typeof(decimal))       return decimal.Parse(raw);
        if (u == typeof(double))        return double.Parse(raw);
        if (u == typeof(bool))          return bool.Parse(raw);
        if (u == typeof(DateTime))      return DateTime.Parse(raw);
        return raw;
    }
}
