using System;
using System.Collections.Generic;
using System.Linq;
using VGStockpile.Transfers.Persistence;

namespace VGStockpile.Transfers.Engine;

internal enum TransferError { None, InsufficientCredits, QueueFull, EmptyManifest }

internal readonly record struct TransferRequestResult(
    bool IsSuccess, TransferError Error, TransferRequest? Created);

internal sealed class TransferEngine
{
    private readonly TransferQueue _queue;
    private readonly IMaterialStorageMutator _mutator;
    private readonly ICreditsMutator _credits;
    private readonly ITransferStore _store;
    private readonly TransferConfig _cfg;
    private string _savePathCurrent;
    private readonly Func<string> _idGen;

    public TransferEngine(
        TransferQueue queue,
        IMaterialStorageMutator mutator,
        ICreditsMutator credits,
        ITransferStore store,
        TransferConfig cfg,
        string savePath,
        Func<string>? idGen = null)
    {
        _queue = queue; _mutator = mutator; _credits = credits;
        _store = store; _cfg = cfg; _savePathCurrent = savePath;
        _idGen = idGen ?? (() => Guid.NewGuid().ToString("N"));
    }

    public IReadOnlyList<TransferRequest> Pending => _queue.Items;

    public TransferRequestResult RequestTransfer(
        string sourceGuid, string destGuid,
        IReadOnlyList<TransferManifestLine> manifest, int jumpDistance)
    {
        if (manifest.Count == 0)
            return new TransferRequestResult(false, TransferError.EmptyManifest, null);
        if (!_queue.CanAccept)
            return new TransferRequestResult(false, TransferError.QueueFull, null);

        var totalUnits = 0; for (var i = 0; i < manifest.Count; i++) totalUnits += manifest[i].Quantity;
        var fee  = FeeCalculator.Compute(jumpDistance, totalUnits, _cfg);
        var eta  = EtaCalculator.ComputeSeconds(jumpDistance, _cfg);

        if (!_credits.TryDebit(fee))
            return new TransferRequestResult(false, TransferError.InsufficientCredits, null);

        _mutator.Reserve(sourceGuid, manifest);

        var req = new TransferRequest(
            Id: _idGen(), SourceStationGuid: sourceGuid, DestStationGuid: destGuid,
            Manifest: manifest, FeeCredits: fee, JumpDistance: jumpDistance,
            TotalSeconds: eta, RemainingSeconds: eta, Status: TransferStatus.Pending);

        _queue.Add(req);
        Flush();
        return new TransferRequestResult(true, TransferError.None, req);
    }

    public bool CancelTransfer(string id)
    {
        var req = _queue.Items.FirstOrDefault(r => r.Id == id);
        if (req is null) return false;
        if (!_queue.Cancel(id)) return false;
        _mutator.Return(req.SourceStationGuid, req.Manifest);
        Flush();
        return true;
    }

    public IReadOnlyList<TransferRequest> Tick(float dt)
    {
        var completed = _queue.TickSeconds(dt);
        if (completed.Count == 0) return Array.Empty<TransferRequest>();

        for (var i = 0; i < completed.Count; i++)
            _mutator.Deliver(completed[i].DestStationGuid, completed[i].Manifest);

        _queue.RemoveTerminal();
        Flush();
        return completed;
    }

    public void LoadFromStore()
    {
        var sidecar = _store.Load(_savePathCurrent);
        _queue.LoadFrom(sidecar.Items);
    }

    public void SetSavePath(string newPath) => _savePathCurrent = newPath;
    public void FlushNow() => Flush();

    private void Flush() =>
        _store.Save(_savePathCurrent, new TransferSidecar(TransferSidecar.CurrentVersion, _queue.Items.ToList()));
}
