using System.Collections.Generic;
using Behaviour.Item;
using Source.Item;
using UnityEngine;

namespace VGStockpile.Data;

internal sealed class MaterialCatalog : IMaterialCatalog
{
    // The static `InventoryItemType.allItems` Dictionary IS publicized in the
    // compile-time stub, but the runtime DLL still ships it as private — Mono
    // throws FieldAccessException on first read. The `all` IEnumerable property
    // exposes the same data through a public getter and IS accessible.
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
            ItemCategory.RefinedProduct  => IsCanister(type)
                                                ? MaterialCategory.RefinedCanister
                                                : MaterialCategory.RefinedGoods,
            ItemCategory.Crystal         => MaterialCategory.Crystal,
            ItemCategory.TradeGoods      => MaterialCategory.TradeGoods,
            ItemCategory.Salvage         => MaterialCategory.Salvage,
            ItemCategory.Junk            => MaterialCategory.Other,
            _                            => MaterialCategory.Unknown,
        };
    }

    private static bool IsCanister(InventoryItemType type)
    {
        // Canister-style refined products are extracted/carryable items like
        // "Canister of Refined Oxide". Match on identifier substring (more
        // stable than localized displayName).
        var id = type.identifier ?? "";
        return id.IndexOf("canister", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public Sprite? Icon(string materialTypeId) => LookupType(materialTypeId)?.icon;

    public InventoryItemType? GetItemType(string materialTypeId) => LookupType(materialTypeId);

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
