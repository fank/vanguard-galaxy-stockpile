using System.Linq;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using VGStockpile.Config;
using VGStockpile.Data;
using VGStockpile.Locate;
using VGStockpile.Patches;
using VGStockpile.UI;

namespace VGStockpile;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInProcess("VanguardGalaxy.exe")]
public class Plugin : BaseUnityPlugin
{
    public const string PluginGuid    = "vgstockpile";
    public const string PluginName    = "Vanguard Galaxy Stockpile";
    public const string PluginVersion = "0.1.0";

    internal static Plugin          Instance { get; private set; } = null!;
    internal static ManualLogSource Log      { get; private set; } = null!;

    internal StockpileConfig         Cfg     { get; private set; } = null!;
    internal MaterialCatalog         Catalog { get; private set; } = null!;
    internal StationStorageReader    Reader  { get; private set; } = null!;
    internal StationLocator          Locator { get; private set; } = null!;
    internal StorageGridBuilder      Builder { get; private set; } = null!;

    private StationStorageIcon?      _icon;
    private StationStorageWindow?    _window;
    private Harmony                  _harmony = null!;

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

        _harmony = new Harmony(PluginGuid);
        _harmony.PatchAll(typeof(HudCanvasReadyPatch));

        var patchCount = _harmony.GetPatchedMethods().Count();
        Log.LogInfo($"{PluginName} v{PluginVersion} loaded ({patchCount} patched method(s))");
    }

    internal void AttachIcon(Canvas hudCanvas)
    {
        if (_icon != null) return;

        var clickHandler = new StationRowClickHandler(
            Locator,
            closeWindow:         () => _window?.Hide(),
            shouldCloseOnLocate: () => Cfg.CloseWindowOnLocate.Value,
            logWarning:          msg => Log.LogWarning(msg));

        _window = StationStorageWindow.Create(
            hudCanvas,
            Builder,
            hideOresDefault: () => Cfg.HideOresByDefault.Value,
            onLabelClick:    snap => clickHandler.Click(snap));

        _icon = StationStorageIcon.Create(
            hudCanvas,
            onClick: ToggleWindow,
            rightPadding: Cfg.IconRightPadding.Value,
            topPadding:   Cfg.IconTopPadding.Value);

        if (Cfg.Verbose.Value)
            Log.LogInfo("VGStockpile icon attached to HUD canvas.");
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

    private void OnDestroy()
    {
        _harmony?.UnpatchSelf();
    }
}
