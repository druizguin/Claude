using Audit.Data.Database;
using Audit.Data.Repositories;
using Audit.Dom.Entities;
using Audit.Dom.Interfaces;
using Audit.Svc;
using Microsoft.Data.Sqlite;

namespace Audit.BDD.Tests.Steps;

public class AuditTestContext
{
    public string DbPath { get; } = System.IO.Path.Combine(
        System.IO.Path.GetTempPath(), $"bdd-{Guid.NewGuid()}.db");

    public IAuditService AuditService { get; private set; } = null!;

    public AuditEntry CurrentEntry { get; set; } = null!;
    public Guid LastCreatedId { get; set; }
    public AuditEntry? RetrievedEntry { get; set; }
    public IEnumerable<AuditEntry> QueryResult { get; set; } = [];
    public int CountResult { get; set; }

    public async Task InitializeAsync()
    {
        var connStr = $"Data Source={DbPath}";
        await new DatabaseInitializer(connStr).InitializeAsync();
        AuditService = new AuditService(new AuditRepository(connStr));
    }

    public void Cleanup()
    {
        // Release all pooled connections so SQLite releases the file lock
        SqliteConnection.ClearAllPools();
        GC.Collect();
        GC.WaitForPendingFinalizers();

        try { if (System.IO.File.Exists(DbPath)) System.IO.File.Delete(DbPath); }
        catch { /* best-effort cleanup */ }
    }
}
