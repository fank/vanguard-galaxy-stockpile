using UnityEngine;
using VGStockpile.Data;

namespace VGStockpile.UI;

/// <summary>
/// Single source of truth for how each <see cref="MaterialCategory"/> is
/// presented — its label, atlas icon, and ordering. Consumed by both the
/// main window's filter strip (<see cref="StationStorageWindow"/>) and the
/// transfer dialog's group headers so the two never drift apart.
/// </summary>
internal static class MaterialCategoryDisplay
{
    public readonly record struct Descriptor(
        MaterialCategory Category,
        string SpriteName,
        int RectX,
        int RectY,
        string Label);

    /// <summary>
    /// Filterable categories, in the order shown in the main view's filter
    /// strip. Sprite rect coordinates match the values dumped by IconDumper
    /// into BepInEx/cache/vgstockpile-icons/manifest.tsv.
    /// </summary>
    public static readonly Descriptor[] Filterable =
    {
        new(MaterialCategory.Ore,             "OreIcons_2",       192, 384, "Ores"),
        new(MaterialCategory.RefinedCanister, "MaterialIcons_0",    0,  96, "Refined Canisters"),
        new(MaterialCategory.RefinedGoods,    "CraftingIcons2_0",   0,   0, "Refined Products"),
        new(MaterialCategory.Crystal,         "CrystalIcons_0",     0,  96, "Crystals"),
        new(MaterialCategory.TradeGoods,      "CraftingIcons_1",   96, 384, "Trade Goods"),
        new(MaterialCategory.Salvage,         "SalvageIcons_0",     0,  96, "Salvage"),
    };

    // Grouping order for the transfer dialog: the filterable categories first
    // (in filter-strip order), then the catch-all buckets that have no filter
    // button. Anything unlisted sorts last.
    private static readonly MaterialCategory[] GroupOrder =
    {
        MaterialCategory.Ore,
        MaterialCategory.RefinedCanister,
        MaterialCategory.RefinedGoods,
        MaterialCategory.Crystal,
        MaterialCategory.TradeGoods,
        MaterialCategory.Salvage,
        MaterialCategory.Other,
        MaterialCategory.Unknown,
    };

    public static int Order(MaterialCategory cat)
    {
        var i = System.Array.IndexOf(GroupOrder, cat);
        return i < 0 ? int.MaxValue : i;
    }

    public static string Label(MaterialCategory cat)
    {
        foreach (var d in Filterable)
            if (d.Category == cat) return d.Label;
        return cat switch
        {
            MaterialCategory.Other   => "Other",
            MaterialCategory.Unknown => "Uncategorized",
            _                        => cat.ToString(),
        };
    }

    public static Sprite? Icon(MaterialCategory cat)
    {
        foreach (var d in Filterable)
            if (d.Category == cat)
                return SpriteLookup.FindByNameAndRect(d.SpriteName, d.RectX, d.RectY)
                       ?? SpriteLookup.FindByName(d.SpriteName);
        return null;
    }
}
