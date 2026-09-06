using System;
using VGModAPI;
using VGStockpile.Transfers.Engine;

namespace VGStockpile.Transfers.Persistence;

internal sealed class CoordinatedTransfers : ITransferPersistence
{
    private readonly ILifecycleApi _lifecycle;
    private readonly TransferEngine? _engine;
    private readonly Action _resetUi;
    private readonly IPersistenceRegistration _registration;
    private readonly IDisposable _subscription;
    private TransferSidecar _retained = TransferSidecar.Empty();
    private Guid? _session;
    private bool _disposed;

    internal CoordinatedTransfers(ILifecycleApi lifecycle, IPersistenceApi api, TransferEngine? engine,
        bool importLegacy, Action<int> disabledPending, Action resetUi, Action<string> warn)
    {
        _lifecycle = lifecycle; _engine = engine; _resetUi = resetUi;
        if (engine != null) { engine.OperationAllowed = () => false; engine.QueryAllowed = () => false; }
        Clear();
        _registration = api.Register(new PersistenceProvider("vgstockpile", 1,
            () => TransferPayloadCodec.Encode(_engine?.Snapshot() ?? _retained),
            (session, payload) =>
            {
                try
                {
                    Clear(); _session = session.Id;
                    bool imported = false;
                    if (payload == null && importLegacy && session.SavePath != null)
                    { payload = TransferPayloadCodec.ReadLegacy(SavePathResolver.Sidecar(session.SavePath)); imported = payload != null; }
                    var state = payload == null ? TransferSidecar.Empty() : TransferPayloadCodec.Decode(payload);
                    _retained = state; _engine?.Restore(state);
                    if (engine == null && state.Items.Count > 0) disabledPending(state.Items.Count);
                    if (imported) warn("Explicit read-only legacy transfer adoption; no historical snapshot consistency inferred.");
                }
                catch (Exception error)
                {
                    warn("Coordinated transfer restore failed: " + error.GetType().Name + ": " + error.Message);
                    throw;
                }
            }, TransferPayloadCodec.IsValid));
        try { _subscription = lifecycle.Subscribe("vgstockpile.coordinated-ui", Observe); }
        catch { _registration.Dispose(); throw; }
        if (engine != null)
        {
            engine.ValidateEtaForPersistence = true;
            engine.OperationAllowed = () => CanOperate;
            engine.QueryAllowed = () => CanOperate;
            engine.UnavailableReason = () => Status is "inactive" or "ready" or "migration-pending"
                ? TransferError.SessionUnavailable : TransferError.PersistenceUnavailable;
        }
    }

    public bool CanOperate => !_disposed && _registration.MutationAllowed;
    internal string Status => _registration.Status;
    private void Clear()
    { _session = null; _retained = TransferSidecar.Empty(); _engine?.Restore(_retained); _resetUi(); }
    private void Observe(LifecycleEvent e)
    {
        if (_disposed || e.Session == null) return;
        if (e.Kind is LifecycleEventKind.SessionStarting or LifecycleEventKind.SessionInvalidated or LifecycleEventKind.SessionStartFailed)
            if (_session == e.Session.Id || _lifecycle.CurrentSession?.Id == e.Session.Id) Clear();
    }
    public void Dispose()
    {
        if (_disposed) return;
        _registration.Dispose(); _disposed = true; _subscription.Dispose(); Clear();
    }
}
