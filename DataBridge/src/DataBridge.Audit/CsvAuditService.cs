using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using DataBridge.Core.Interfaces;
using DataBridge.Core.Models;
using DataBridge.Core.Query;

namespace DataBridge.Audit;

/// <summary>
/// Persists audit records to a CSV file. Thread-safe via a SemaphoreSlim.
/// Supports queries with the same QuerySpec filtering/sorting/paging as other connectors.
/// </summary>
public class CsvAuditService : IAuditService
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public CsvAuditService(string filePath)
    {
        _filePath = filePath;
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }

    public async Task LogAsync(AuditRecord record, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var config   = new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = !File.Exists(_filePath) };
            await using var writer = new StreamWriter(_filePath, append: true);
            await using var csv    = new CsvWriter(writer, config);
            csv.WriteRecord(record);
            await csv.NextRecordAsync();
        }
        finally { _lock.Release(); }
    }

    public Task<QueryResult<AuditRecord>> QueryAsync(QuerySpec spec, CancellationToken ct = default)
    {
        var all      = LoadAll();
        var filtered = FilterEvaluator.Apply(all, spec.Filter).ToList();
        var sorted   = SortingEngine.Apply(filtered, spec.OrderBy).ToList();

        int from   = spec.Page?.From ?? 0;
        int offset = spec.Page?.Offset ?? sorted.Count;

        return Task.FromResult(new QueryResult<AuditRecord>
        {
            Items      = sorted.Skip(from).Take(offset).ToList(),
            TotalCount = sorted.Count,
            From       = from,
            Offset     = offset
        });
    }

    public Task<IReadOnlyList<AuditRecord>> GetByEntityIdAsync(Guid entityId, CancellationToken ct = default)
    {
        var result = LoadAll().Where(r => r.EntityId == entityId).ToList();
        return Task.FromResult<IReadOnlyList<AuditRecord>>(result);
    }

    private List<AuditRecord> LoadAll()
    {
        if (!File.Exists(_filePath)) return new List<AuditRecord>();

        var config = new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true };
        using var reader = new StreamReader(_filePath);
        using var csv    = new CsvReader(reader, config);
        return csv.GetRecords<AuditRecord>().ToList();
    }
}
