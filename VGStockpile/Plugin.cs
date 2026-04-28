using BepInEx;
using BepInEx.Logging;

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

    private void Awake()
    {
        Instance = this;
        Log      = Logger;
        Log.LogInfo($"{PluginName} v{PluginVersion} loaded");
    }
}
