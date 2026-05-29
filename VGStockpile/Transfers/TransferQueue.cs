using System;
using System.Collections.Generic;
using System.Linq;

namespace VGStockpile.Transfers;

internal sealed class TransferQueueFullException : Exception
{
    public TransferQueueFullException(int max)
        : base($"Transfer queue is full ({max} pending).") { }
}

internal sealed class TransferQueue
{
    private readonly List<TransferRequest> _items = new();
    private readonly int _maxConcurrent;

    public TransferQueue(int maxConcurrent) { _maxConcurrent = maxConcurrent; }

    public IReadOnlyList<TransferRequest> Items => _items;
    public int PendingCount => _items.Count(r => r.Status == TransferStatus.Pending);
    public bool CanAccept => _maxConcurrent == 0 || PendingCount < _maxConcurrent;

    public TransferRequest Add(TransferRequest req)
    {
        if (!CanAccept) throw new TransferQueueFullException(_maxConcurrent);
        _items.Add(req);
        return req;
    }

    public bool Cancel(string id)
    {
        var idx = _items.FindIndex(r => r.Id == id);
        if (idx < 0) return false;
        var r = _items[idx];
        if (r.Status != TransferStatus.Pending) return false;
        if (r.RemainingSeconds <= 0f) return false;
        _items.RemoveAt(idx);
        return true;
    }

    /// <summary>
    /// Decrements RemainingSeconds on every Pending request. Returns the
    /// just-completed list (status flipped to Completed and Remaining floored
    /// to 0). Items remain in <see cref="Items"/> until drained externally.
    /// </summary>
    public IReadOnlyList<TransferRequest> TickSeconds(float dt)
    {
        if (dt <= 0f) return Array.Empty<TransferRequest>();

        var justCompleted = new List<TransferRequest>();
        for (var i = 0; i < _items.Count; i++)
        {
            var r = _items[i];
            if (r.Status != TransferStatus.Pending) continue;

            var newRemaining = r.RemainingSeconds - dt;
            if (newRemaining <= 0f)
            {
                var completed = r with { RemainingSeconds = 0f, Status = TransferStatus.Completed };
                _items[i] = completed;
                justCompleted.Add(completed);
            }
            else
            {
                _items[i] = r with { RemainingSeconds = newRemaining };
            }
        }
        return justCompleted;
    }

    /// <summary>Replaces the queue contents — used when loading from sidecar.</summary>
    public void LoadFrom(IEnumerable<TransferRequest> snapshot)
    {
        _items.Clear();
        _items.AddRange(snapshot);
    }

    /// <summary>Removes Completed and Cancelled items (post-delivery housekeeping).</summary>
    public void RemoveTerminal()
    {
        _items.RemoveAll(r => r.Status != TransferStatus.Pending);
    }
}
