using BepInEx.Configuration;

namespace VGStockpile.Config;

internal sealed class StockpileConfig
{
    public ConfigEntry<bool>  HideOresByDefault   { get; }
    public ConfigEntry<float> IconRightPadding    { get; }
    public ConfigEntry<float> IconTopPadding      { get; }
    public ConfigEntry<bool>  CloseWindowOnLocate { get; }
    public ConfigEntry<bool>  Verbose             { get; }

    public StockpileConfig(ConfigFile cfg)
    {
        HideOresByDefault = cfg.Bind("UI", "HideOresByDefault", true,
            "Initial state of the 'Hide ores' toggle when the window opens.");
        IconRightPadding = cfg.Bind("UI", "IconRightPadding", 24f,
            "Pixels of padding from the right edge of the screen for the HUD icon.");
        IconTopPadding = cfg.Bind("UI", "IconTopPadding", 12f,
            "Pixels of padding from the top edge of the screen for the HUD icon.");
        CloseWindowOnLocate = cfg.Bind("UI", "CloseWindowOnLocate", true,
            "When clicking a station label, close the stockpile window after focusing the map.");
        Verbose = cfg.Bind("Diagnostics", "Verbose", false,
            "Enable verbose logging.");
    }
}
