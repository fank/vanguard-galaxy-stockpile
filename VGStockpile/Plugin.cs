using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Source.Galaxy;
using Source.Galaxy.POI;
using UnityEngine;
using VGStockpile.Config;
using VGStockpile.Data;
using VGStockpile.Locate;
using VGStockpile.Patches;
using VGStockpile.Transfers;
using VGStockpile.Transfers.Engine;
using VGStockpile.Transfers.Persistence;
using VGStockpile.UI;
using VGStockpile.UI.Refinery;
using VGStockpile.UI.Transfers;

namespace VGStockpile;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInProcess("VanguardGalaxy.exe")]
public class Plugin : BaseUnityPlugin
{
    public const string PluginGuid    = "vgstockpile";
    public const string PluginName    = "Vanguard Galaxy Stockpile";
    public const string PluginVersion = "0.3.0";

    internal static Plugin          Instance { get; private set; } = null!;
    internal static ManualLogSource Log      { get; private set; } = null!;

    internal StockpileConfig         Cfg     { get; private set; } = null!;
    internal MaterialCatalog         Catalog { get; private set; } = null!;
    internal StationStorageReader    Reader  { get; private set; } = null!;
    internal StationLocator          Locator { get; private set; } = null!;
    internal StorageGridBuilder      Builder { get; private set; } = null!;
    internal RefineryJobReader       RefineryReader  { get; private set; } = null!;
    internal RefineryJobsBuilder     RefineryBuilder { get; private set; } = null!;

    private StationStorageIcon?      _icon;
    private StationStorageWindow?    _window;
    private RefineryJobsIcon?        _refineryIcon;
    private RefineryJobsWindow?      _refineryWindow;
    private Canvas?                  _hudCanvas;
    private Harmony                  _harmony = null!;

    internal TransferEngine?          _engine;
    internal MaterialStorageMutator?  _mutator;
    internal CreditsMutator?          _credits;
    internal StationContextAdapter?   _ctxAdapter;

    public bool IconAttached => _icon != null;

    private void Awake()
    {
        Instance = this;
        Log      = Logger;

        Cfg     = new StockpileConfig(Config);
        Catalog = new MaterialCatalog();
        Reader  = new StationStorageReader(Log);
        Locator = new StationLocator(Log);
        Builder = new StorageGridBuilder(Catalog);
        RefineryReader  = new RefineryJobReader(Log);
        RefineryBuilder = new RefineryJobsBuilder(Catalog);

        if (Cfg.TransfersEnabled.Value)
        {
            var tcfg     = Cfg.ToTransferConfig();
            _mutator     = new MaterialStorageMutator(Log);
            _credits     = new CreditsMutator(Log);
            _ctxAdapter  = new StationContextAdapter(Log);
            var queue    = new TransferQueue(tcfg.MaxConcurrent);
            var store    = new JsonTransferStore(msg => Log.LogWarning(msg));
            _engine      = new TransferEngine(
                queue, _mutator, _credits, store, tcfg, savePath: "");
            TransferEngineDriver.Attach(gameObject, _engine,
                onCompleted: req =>
                {
                    var dest  = ResolveStationName(req.DestStationGuid);
                    var lines = string.Join(", ",
                        req.Manifest.Select(l => $"{l.Quantity} {l.ItemIdentifier}"));
                    Notifications.Toast($"Transfer complete at {dest}: {lines}.");
                });
            Log.LogInfo("VGStockpile transfers enabled.");
        }

        // Vanilla's HUD canvas + side menu come up at unpredictable times
        // (depending on how long the user lingers in the main menu before
        // loading a save). Patch SidePanel.Start as the trigger — it runs
        // exactly once per gameplay-scene load, after the side menu's
        // canvas hierarchy is built, with no race or timeout.
        _harmony = new Harmony(PluginGuid);
        _harmony.PatchAll(typeof(SidePanelReadyPatch));
        _harmony.PatchAll(typeof(SaveLoadPatch));   // unconditional — needed for §6.8 disable-mid-flight peek
        if (_engine is not null)
        {
            _harmony.PatchAll(typeof(SaveWritePatch));
        }

        Log.LogInfo($"{PluginName} v{PluginVersion} loaded; waiting for SidePanel.");
    }

    private void OnDestroy() => _harmony?.UnpatchSelf();

    internal void AttachIcon(Canvas hudCanvas)
    {
        if (_icon != null) return;
        _hudCanvas = hudCanvas;

        var clickHandler = new StationRowClickHandler(
            Locator,
            closeWindow:         () => _window?.Hide(),
            shouldCloseOnLocate: () => Cfg.CloseWindowOnLocate.Value,
            logWarning:          msg => Log.LogWarning(msg));

        var transfersEnabled = Cfg.TransfersEnabled.Value && _engine is not null;
        TransferConfig? transferCfg = transfersEnabled ? Cfg.ToTransferConfig() : null;

        _window = StationStorageWindow.Create(
            hudCanvas,
            Builder,
            Catalog,
            initialActive:    () => Cfg.GetActive(),
            onActiveChanged:  active => Cfg.SetActive(active),
            onLabelClick:     snap => clickHandler.Click(snap),
            transfersEnabled: transfersEnabled,
            transferCfg:      transferCfg,
            getStationContext: transfersEnabled ? guid => BuildStationContextFor(guid) : null,
            onPullClick:      transfersEnabled ? snap => OpenTransferDialog(snap, TransferDirection.Pull)  : null,
            onPushClick:      transfersEnabled ? snap => OpenTransferDialog(snap, TransferDirection.Push) : null,
            getPending:               transfersEnabled ? () => _engine!.Pending : null,
            onCancelTransfer:         transfersEnabled ? id  => _engine!.CancelTransfer(id) : null,
            onLocateByGuid:           transfersEnabled ? guid => Locator.LocateByGuid(guid) : null,
            stationDisplayNameByGuid: transfersEnabled ? ResolveStationName : null);

        _icon = StationStorageIcon.Create(
            hudCanvas,
            onClick: ToggleWindow,
            rightPadding: Cfg.IconRightPadding.Value,
            topPadding:   Cfg.IconTopPadding.Value,
            log:          Log);

        _refineryWindow = RefineryJobsWindow.Create(
            hudCanvas, RefineryBuilder, Catalog,
            capture: () => RefineryReader.CaptureAll(),
            onStationClick: guid =>
            {
                Locator.LocateByGuid(guid);
                if (Cfg.CloseWindowOnLocate.Value) _refineryWindow?.Hide();
            },
            log: Log);

        // Place the refinery-jobs icon to the left of the stockpile icon
        // (icons are 40px wide; +48 leaves an 8px gap), same top edge.
        _refineryIcon = RefineryJobsIcon.Create(
            hudCanvas,
            onClick: ToggleRefineryWindow,
            rightPadding: Cfg.IconRightPadding.Value + 48f,
            topPadding:   Cfg.IconTopPadding.Value,
            log:          Log);

        Log.LogInfo($"VGStockpile icon attached to canvas '{hudCanvas.name}'.");
    }

    private StationContext BuildStationContextFor(string guid)
    {
        if (_ctxAdapter is null) return default;
        var data = GalaxyMapData.current;
        if (data is null) return default;
        SpaceStation? found = null;
        foreach (var poi in data.allPointsOfInterest)
        {
            if (poi is SpaceStation st && st.guid == guid)
            {
                found = st;
                break;
            }
        }
        if (found is null) return default;
        return _ctxAdapter.FromStation(found, SpaceStation.current);
    }

    private string ResolveStationName(string guid)
    {
        if (string.IsNullOrEmpty(guid)) return guid;

        // Walk all POIs directly — Reader.CaptureAll() filters out stations
        // with empty material storage, so a reservation that drains a source
        // would otherwise leave that station's name unresolved mid-flight.
        var data = GalaxyMapData.current;
        if (data is null) return guid;
        foreach (var poi in data.allPointsOfInterest)
        {
            if (poi is not SpaceStation st) continue;
            if (st.guid != guid) continue;
            return st.name ?? guid;
        }
        return guid;
    }

    private void OpenTransferDialog(StationStorageSnapshot snap, TransferDirection dir)
    {
        if (_engine is null || _ctxAdapter is null) return;
        if (_hudCanvas is null) { Log.LogWarning("Cannot open dialog: no HUD canvas."); return; }

        var current = SpaceStation.current;
        if (current is null)
        {
            // Defensive: row buttons gate this via EligibilityRules.CanPull/PushFrom
            // requiring IsPlayerDocked, but if a stale click slips through we
            // refuse to open the dialog and toast.
            UI.Notifications.Toast("Transfers require docking at a station.");
            return;
        }

        var allSnaps = Reader.CaptureAll();
        string fromName, toName;
        IReadOnlyDictionary<string, int> sourceStock;
        int jumpDistance;

        var currentName = current.name ?? "";

        if (dir == TransferDirection.Pull)
        {
            fromName     = snap.StationName;
            toName       = currentName;

            // Re-read live source stock (mirrors the Push branch below) instead
            // of the row snapshot captured at window-open. Otherwise the dialog
            // would still offer ore that has already left the station — e.g. a
            // prior in-flight transfer reserved it — and the user would pick
            // amounts that CommitTransfer then clamps and refuses. Falls back to
            // empty (not the stale snapshot) when the source now reads empty.
            StationStorageSnapshot? sourceSnap = null;
            foreach (var s in allSnaps)
            {
                if (s.StationId == snap.StationId) { sourceSnap = s; break; }
            }
            sourceStock  = sourceSnap?.Items ?? new Dictionary<string, int>();
            jumpDistance = ComputeJumpDistance(snap.SystemGuid, current?.system?.guid);
        }
        else
        {
            fromName    = currentName;
            toName      = snap.StationName;

            StationStorageSnapshot? currentSnap = null;
            var currentGuid = current?.guid;
            if (currentGuid is not null)
            {
                foreach (var s in allSnaps)
                {
                    if (s.StationId == currentGuid) { currentSnap = s; break; }
                }
            }
            sourceStock  = currentSnap?.Items ?? new Dictionary<string, int>();
            jumpDistance = ComputeJumpDistance(current?.system?.guid, snap.SystemGuid);
        }

        TransferDialog.Open(
            _hudCanvas.transform,
            dir, fromName, toName,
            sourceStock, Cfg.ToTransferConfig(), jumpDistance,
            Catalog,
            onConfirmRequest: manifest => CommitTransfer(snap, dir, manifest),
            onCancel: () => Log.LogDebug($"{dir} dialog cancelled."));
    }

    private TransferDialogOutcome CommitTransfer(
        StationStorageSnapshot snap, TransferDirection dir,
        IReadOnlyList<TransferManifestLine> manifest)
    {
        if (_engine is null) return new TransferDialogOutcome(false, "Engine unavailable.");

        string sourceGuid, destGuid;
        int jumpDistance;
        var current = SpaceStation.current;
        var currentGuid = current?.guid ?? "";

        if (dir == TransferDirection.Pull)
        {
            sourceGuid   = snap.StationId;
            destGuid     = currentGuid;
            jumpDistance = ComputeJumpDistance(snap.SystemGuid, current?.system?.guid);
        }
        else
        {
            sourceGuid   = currentGuid;
            destGuid     = snap.StationId;
            jumpDistance = ComputeJumpDistance(current?.system?.guid, snap.SystemGuid);
        }

        if (string.IsNullOrEmpty(sourceGuid) || string.IsNullOrEmpty(destGuid))
            return new TransferDialogOutcome(false, "Invalid station selection.");

        // Fresh source stock for re-validation.
        var live = Reader.CaptureAll();
        var liveSource = live.FirstOrDefault(s => s.StationId == sourceGuid);
        if (liveSource is null)
            return new TransferDialogOutcome(false, "Source station unavailable.");

        // Clamp manifest against live availability; if any line was clamped, refuse + banner.
        var clamped = new List<TransferManifestLine>(manifest.Count);
        var anyClamped = false;
        for (var i = 0; i < manifest.Count; i++)
        {
            var line = manifest[i];
            var avail = liveSource.Items.TryGetValue(line.ItemIdentifier, out var n) ? n : 0;
            if (avail < line.Quantity) anyClamped = true;
            var qty = avail < line.Quantity ? avail : line.Quantity;
            if (qty > 0) clamped.Add(new TransferManifestLine(line.ItemIdentifier, qty));
        }
        if (anyClamped)
            return new TransferDialogOutcome(false, $"Stock changed at {liveSource.StationName}; reduce and retry.");
        if (clamped.Count == 0)
            return new TransferDialogOutcome(false, "Nothing left to transfer.");

        var result = _engine.RequestTransfer(sourceGuid, destGuid, clamped, jumpDistance);
        if (!result.IsSuccess)
        {
            var msg = result.Error switch
            {
                TransferError.InsufficientCredits => "Insufficient credits.",
                TransferError.QueueFull           => "Transfer queue full.",
                TransferError.EmptyManifest       => "Select at least one item.",
                _                                 => "Could not queue transfer.",
            };
            return new TransferDialogOutcome(false, msg);
        }

        return new TransferDialogOutcome(true, null);
    }

    private static int ComputeJumpDistance(string? fromSystemGuid, string? toSystemGuid)
    {
        if (string.IsNullOrEmpty(fromSystemGuid) || string.IsNullOrEmpty(toSystemGuid)) return 0;
        if (fromSystemGuid == toSystemGuid) return 0;

        var data = GalaxyMapData.current;
        if (data is null) return 0;

        SystemMapData? from = null;
        foreach (var s in data.allSystems)
        {
            if (s?.guid == fromSystemGuid) { from = s; break; }
        }
        if (from is null) return 0;

        var dists = JumpDistances.ComputeFrom(from);
        return dists.TryGetValue(toSystemGuid, out var d) ? d : 0;
    }

    private void ToggleWindow()
    {
        if (_window is null) return;
        try
        {
            var snapshots = Reader.CaptureAll();
            _window.Toggle(snapshots);
        }
        catch (System.Exception ex)
        {
            Log.LogError($"Failed to capture station storage: {ex}");
        }
    }

    private void ToggleRefineryWindow()
    {
        if (_refineryWindow is null) return;
        try
        {
            var jobs = RefineryReader.CaptureAll();
            _refineryWindow.Toggle(jobs);
        }
        catch (System.Exception ex)
        {
            Log.LogError($"Failed to capture refinery jobs: {ex}");
        }
    }

    internal void OnSaveLoaded(string savePath)
    {
        var sidecarPath = Transfers.Persistence.SavePathResolver.Sidecar(savePath);

        if (_engine is not null)
        {
            // Transfers are enabled: load and resume.
            _engine.SetSavePath(sidecarPath);
            _engine.LoadFromStore();
            return;
        }

        // Transfers are disabled — peek the sidecar to honor spec §6.8.
        try
        {
            var store = new Transfers.Persistence.JsonTransferStore(msg => Log.LogWarning(msg));
            var sidecar = store.Load(sidecarPath);
            if (sidecar.Items.Count > 0)
            {
                Notifications.Toast(
                    $"VGStockpile transfers disabled — {sidecar.Items.Count} pending transfers will not deliver until re-enabled.");
            }
        }
        catch (System.Exception ex)
        {
            Log.LogWarning($"Disable-mid-flight check failed: {ex.Message}");
        }
    }

    internal void OnSaveWritten(string savePath)
    {
        if (_engine is null) return;
        var sidecarPath = Transfers.Persistence.SavePathResolver.Sidecar(savePath);
        _engine.SetSavePath(sidecarPath);
        _engine.FlushNow();
    }
}
