using Dapper;
using Microsoft.Data.Sqlite;

namespace Audit.Data.Database;

public class DatabaseInitializer
{
    private readonly string _connectionString;

    public DatabaseInitializer(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task InitializeAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await connection.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS AuditEntries (
                Id          TEXT NOT NULL PRIMARY KEY,
                UserId      TEXT NOT NULL,
                EntityId    TEXT NOT NULL,
                EntityName  TEXT NOT NULL,
                Action      INTEGER NOT NULL,
                Timestamp   TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS IX_AuditEntries_UserId ON AuditEntries(UserId);
            CREATE INDEX IF NOT EXISTS IX_AuditEntries_Timestamp ON AuditEntries(Timestamp);

            CREATE TABLE IF NOT EXISTS AuditDetails (
                Id           TEXT NOT NULL PRIMARY KEY,
                AuditId      TEXT NOT NULL,
                PropertyName TEXT NOT NULL,
                OldValue     TEXT,
                NewValue     TEXT,
                FOREIGN KEY (AuditId) REFERENCES AuditEntries(Id)
            );

            CREATE INDEX IF NOT EXISTS IX_AuditDetails_AuditId ON AuditDetails(AuditId);
            """);
    }
}
