using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using VGStockpile.Data;

namespace VGStockpile.Config;

internal sealed class StockpileConfig
{
    public ConfigEntry<string> ActiveCategories    { get; }
    public ConfigEntry<float>  IconRightPadding    { get; }
    public ConfigEntry<float>  IconTopPadding      { get; }
    public ConfigEntry<bool>   CloseWindowOnLocate { get; }
    public ConfigEntry<bool>   Verbose             { get; }
    public ConfigEntry<bool>   DumpIconsOnce       { get; }

    // Categories visible by default: everything except Ores. Salvage included
    // since users explicitly asked to surface it.
    private static readonly MaterialCategory[] DefaultActive =
    {
        MaterialCategory.Refined,
        MaterialCategory.Crystal,
        MaterialCategory.TradeGoods,
        MaterialCategory.Salvage,
        MaterialCategory.Other,
    };

    public StockpileConfig(ConfigFile cfg)
    {
        ActiveCategories = cfg.Bind(
            "UI", "ActiveCategories",
            string.Join(",", DefaultActive.Select(c => c.ToString())),
            "Comma-separated list of MaterialCategory names visible in the grid. " +
            "Toggling a filter button updates this. Valid values: " +
            "Ore, Refined, Crystal, TradeGoods, Salvage, Other.");
        IconRightPadding = cfg.Bind("UI", "IconRightPadding", 24f,
            "Pixels of padding from the right edge of the screen for the HUD icon.");
        IconTopPadding = cfg.Bind("UI", "IconTopPadding", 12f,
            "Pixels of padding from the top edge of the screen for the HUD icon.");
        CloseWindowOnLocate = cfg.Bind("UI", "CloseWindowOnLocate", true,
            "When clicking a station label, close the stockpile window after focusing the map.");
        Verbose = cfg.Bind("Diagnostics", "Verbose", false,
            "Enable verbose logging.");
        DumpIconsOnce = cfg.Bind("Diagnostics", "DumpIconsOnce", false,
            "When true, dump every loaded Sprite to BepInEx/cache/vgstockpile-icons/ " +
            "as PNG (with manifest.tsv) ~8s after game start, then flip this back " +
            "to false. Use to browse all available icons when picking one for the " +
            "HUD button.");
    }

    public HashSet<MaterialCategory> GetActive()
    {
        var set = new HashSet<MaterialCategory>();
        foreach (var part in ActiveCategories.Value.Split(','))
        {
            var trimmed = part.Trim();
            if (trimmed.Length == 0) continue;
            if (System.Enum.TryParse<MaterialCategory>(trimmed, ignoreCase: true, out var cat))
                set.Add(cat);
        }
        return set;
    }

    public void SetActive(IEnumerable<MaterialCategory> active)
    {
        ActiveCategories.Value = string.Join(",", active.OrderBy(c => (int)c).Select(c => c.ToString()));
    }
}
