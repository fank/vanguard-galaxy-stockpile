using Behaviour.Item;
using Source.Item;

namespace VGStockpile.Data;

internal sealed class MaterialCatalog : IMaterialCatalog
{
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

    private static InventoryItemType? LookupType(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (InventoryItemType.allItems is null) return null;
        return InventoryItemType.allItems.TryGetValue(id, out var t) ? t : null;
    }
}
