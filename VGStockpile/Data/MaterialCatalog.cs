using System.Collections.Generic;
using Behaviour.Item;
using Source.Item;

namespace VGStockpile.Data;

internal sealed class MaterialCatalog : IMaterialCatalog
{
    // The static `InventoryItemType.allItems` Dictionary IS publicized in the
    // compile-time stub, but the runtime DLL still ships it as private — Mono
    // throws FieldAccessException on first read. The `all` IEnumerable property
    // exposes the same data through a public getter and IS accessible. We
    // iterate it lazily on first lookup and memoise; the registry is fixed at
    // load time.
    private Dictionary<string, InventoryItemType>? _cache;

    public string DisplayName(string materialTypeId)
    {
        var type = LookupType(materialTypeId);
        return string.IsNullOrEmpty(type?.displayName) ? materialTypeId : type!.displayName;
    }

    public MaterialCategory Category(string materialTypeId)
    {
        var type = LookupType(materialTypeId);
        if (type is null) return MaterialCategory.Unknown;

        return type.itemCategory switch
        {
            ItemCategory.Ore             => MaterialCategory.Ore,
            ItemCategory.RefinedProduct  => MaterialCategory.Refined,
            ItemCategory.Crystal
                or ItemCategory.TradeGoods
                or ItemCategory.Salvage
                or ItemCategory.Junk     => MaterialCategory.Component,
            _                            => MaterialCategory.Unknown,
        };
    }

    private InventoryItemType? LookupType(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        var cache = _cache ??= BuildCache();
        return cache.TryGetValue(id, out var t) ? t : null;
    }

    private static Dictionary<string, InventoryItemType> BuildCache()
    {
        var dict = new Dictionary<string, InventoryItemType>();
        var all = InventoryItemType.all;
        if (all is null) return dict;
        foreach (var t in all)
        {
            if (t is null || string.IsNullOrEmpty(t.identifier)) continue;
            dict[t.identifier] = t;
        }
        return dict;
    }
}
