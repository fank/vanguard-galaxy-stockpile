namespace VGStockpile.Data;

internal enum MaterialCategory
{
    Unknown    = 0,
    Ore,
    Refined,    // ItemCategory.RefinedProduct (canisters/ingots)
    Crystal,    // ItemCategory.Crystal
    TradeGoods, // ItemCategory.TradeGoods (crating items)
    Salvage,    // ItemCategory.Salvage
    Other,      // ItemCategory.Junk / anything else recognised
}
