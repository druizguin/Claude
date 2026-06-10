using Audit.Dom.Entities;
using Audit.Dom.Enums;
using Audit.Dom.Interfaces;
using Dapper;
using Microsoft.Data.Sqlite;

namespace Audit.Data.Repositories;

public class AuditRepository : IAuditRepository
{
    private readonly string _connectionString;

    public AuditRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<Guid> CreateAsync(AuditEntry entry, CancellationToken ct = default)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        using var transaction = await connection.BeginTransactionAsync(ct);

        try
        {
            await connection.ExecuteAsync(
                """
                INSERT INTO AuditEntries (Id, UserId, EntityId, EntityName, Action, Timestamp)
                VALUES (@Id, @UserId, @EntityId, @EntityName, @Action, @Timestamp)
                """,
                new
                {
                    Id = entry.Id.ToString(),
                    entry.UserId,
                    EntityId = entry.EntityId.ToString(),
                    entry.EntityName,
                    Action = (int)entry.Action,
                    Timestamp = entry.Timestamp.ToString("O")
                },
                transaction);

            foreach (var detail in entry.Details)
            {
                await connection.ExecuteAsync(
                    """
                    INSERT INTO AuditDetails (Id, AuditId, PropertyName, OldValue, NewValue)
                    VALUES (@Id, @AuditId, @PropertyName, @OldValue, @NewValue)
                    """,
                    new
                    {
                        Id = detail.Id.ToString(),
                        AuditId = entry.Id.ToString(),
                        detail.PropertyName,
                        detail.OldValue,
                        detail.NewValue
                    },
                    transaction);
            }

            await transaction.CommitAsync(ct);
            return entry.Id;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<AuditEntry?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);

        var entry = await connection.QueryFirstOrDefaultAsync<AuditEntryRow>(
            "SELECT * FROM AuditEntries WHERE Id = @Id",
            new { Id = id.ToString() });

        if (entry is null) return null;

        var details = await connection.QueryAsync<AuditDetailRow>(
            "SELECT * FROM AuditDetails WHERE AuditId = @AuditId",
            new { AuditId = id.ToString() });

        return MapEntry(entry, details);
    }

    public async Task<IEnumerable<AuditEntry>> GetByUserIdAsync(string userId, int skip = 0, int take = 50, CancellationToken ct = default)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);

        var entries = await connection.QueryAsync<AuditEntryRow>(
            "SELECT * FROM AuditEntries WHERE UserId = @UserId ORDER BY Timestamp DESC LIMIT @Take OFFSET @Skip",
            new { UserId = userId, Take = take, Skip = skip });

        return await EnrichWithDetails(connection, entries);
    }

    public async Task<IEnumerable<AuditEntry>> GetAllAsync(int skip = 0, int take = 50, CancellationToken ct = default)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);

        var entries = await connection.QueryAsync<AuditEntryRow>(
            "SELECT * FROM AuditEntries ORDER BY Timestamp DESC LIMIT @Take OFFSET @Skip",
            new { Take = take, Skip = skip });

        return await EnrichWithDetails(connection, entries);
    }

    public async Task<int> CountByUserIdAsync(string userId, CancellationToken ct = default)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        return await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM AuditEntries WHERE UserId = @UserId",
            new { UserId = userId });
    }

    public async Task<int> CountAllAsync(CancellationToken ct = default)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        return await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM AuditEntries");
    }

    private static async Task<IEnumerable<AuditEntry>> EnrichWithDetails(SqliteConnection connection, IEnumerable<AuditEntryRow> entries)
    {
        var entryList = entries.ToList();
        if (!entryList.Any()) return [];

        var ids = entryList.Select(e => e.Id).ToList();
        var inClause = string.Join(",", ids.Select((_, i) => $"@Id{i}"));

        var parameters = new DynamicParameters();
        for (int i = 0; i < ids.Count; i++)
            parameters.Add($"Id{i}", ids[i]);

        var allDetails = await connection.QueryAsync<AuditDetailRow>(
            $"SELECT * FROM AuditDetails WHERE AuditId IN ({inClause})",
            parameters);

        var detailsByAuditId = allDetails.GroupBy(d => d.AuditId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return entryList.Select(e => MapEntry(e,
            detailsByAuditId.TryGetValue(e.Id, out var d) ? d : []));
    }

    private static AuditEntry MapEntry(AuditEntryRow row, IEnumerable<AuditDetailRow> details) =>
        new()
        {
            Id = Guid.Parse(row.Id),
            UserId = row.UserId,
            EntityId = Guid.Parse(row.EntityId),
            EntityName = row.EntityName,
            Action = (AuditAction)(int)row.Action,
            Timestamp = DateTime.Parse(row.Timestamp),
            Details = details.Select(d => new AuditDetail
            {
                Id = Guid.Parse(d.Id),
                AuditId = Guid.Parse(d.AuditId),
                PropertyName = d.PropertyName,
                OldValue = d.OldValue,
                NewValue = d.NewValue
            }).ToList()
        };

    // SQLite INTEGER comes back as Int64, so use long for Action
    private class AuditEntryRow
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public long Action { get; set; }
        public string Timestamp { get; set; } = string.Empty;
    }

    private class AuditDetailRow
    {
        public string Id { get; set; } = string.Empty;
        public string AuditId { get; set; } = string.Empty;
        public string PropertyName { get; set; } = string.Empty;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
    }
}
