using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using VGStockpile.Config;
using VGStockpile.Data;
using VGStockpile.Locate;
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

    public bool IconAttached => _icon != null;

    private void Awake()
    {
        Instance = this;
        Log      = Logger;

        Cfg     = new StockpileConfig(Config);
        Catalog = new MaterialCatalog();
        Reader  = new StationStorageReader(Log, () => Cfg.Verbose.Value);
        Locator = new StationLocator(Log);
        Builder = new StorageGridBuilder(Catalog);

        // Vanilla's HUD canvas + side menu come up after our plugin's Awake
        // and at unpredictable times depending on save load. A polling scout
        // searches for the side-menu tab labels ("Cargo" / "Armory" /
        // "Materials") and gives us the right anchor canvas once they exist.
        HudAnchorScout.Begin(
            onFound:  AttachIcon,
            log:      Log,
            verbose:  Cfg.Verbose.Value);

        Log.LogInfo($"{PluginName} v{PluginVersion} loaded; waiting for HUD anchor.");
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
            Catalog,
            hideOresDefault: () => Cfg.HideOresByDefault.Value,
            onLabelClick:    snap => clickHandler.Click(snap),
            verbose:         () => Cfg.Verbose.Value,
            log:             msg => Log.LogDebug(msg));

        _icon = StationStorageIcon.Create(
            hudCanvas,
            onClick: ToggleWindow,
            rightPadding: Cfg.IconRightPadding.Value,
            topPadding:   Cfg.IconTopPadding.Value);

        Log.LogInfo($"VGStockpile icon attached to canvas '{hudCanvas.name}'.");
    }

    private void ToggleWindow()
    {
        if (_window is null) return;
        try
        {
            // Reader logs its own per-station summary; no extra log here.
            var snapshots = Reader.CaptureAll();
            _window.Toggle(snapshots);
        }
        catch (System.Exception ex)
        {
            Log.LogError($"Failed to capture station storage: {ex}");
        }
    }
}
