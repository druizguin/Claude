using DataBridge.Core.Interfaces;

namespace DataBridge.UnitOfWork;

/// <summary>
/// Coordinates a distributed transaction across multiple data sources using a
/// simplified 2-phase commit (begin → all-commit or all-rollback on failure).
/// If any participant fails during commit the coordinator rolls back all others.
/// </summary>
public class DistributedTransactionCoordinator
{
    private readonly List<ITransactionParticipant> _participants;
    private bool _active;

    public DistributedTransactionCoordinator(IEnumerable<ITransactionParticipant> participants)
        => _participants = participants.ToList();

    public async Task BeginAsync(CancellationToken ct = default)
    {
        if (_active) throw new InvalidOperationException("Transaction already active.");
        await Task.WhenAll(_participants.Select(p => p.BeginTransactionAsync(ct)));
        _active = true;
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (!_active) return;

        var committed = new List<ITransactionParticipant>();
        try
        {
            foreach (var p in _participants)
            {
                await p.CommitAsync(ct);
                committed.Add(p);
            }
        }
        catch
        {
            // Compensate: rollback all already-committed participants
            var compensate = _participants.Except(committed).ToList();
            compensate.AddRange(committed);
            foreach (var p in compensate)
            {
                try { await p.RollbackAsync(ct); } catch { /* best-effort */ }
            }
            throw;
        }
        finally
        {
            _active = false;
        }
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (!_active) return;
        await Task.WhenAll(_participants.Select(async p =>
        {
            try { await p.RollbackAsync(ct); } catch { /* best-effort */ }
        }));
        _active = false;
    }
}
