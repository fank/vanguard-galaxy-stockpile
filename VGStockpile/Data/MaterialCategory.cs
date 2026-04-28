namespace VGStockpile.Data;

internal enum MaterialCategory
{
    Unknown          = 0,
    Ore,              // ItemCategory.Ore
    RefinedCanister,  // RefinedProduct items whose identifier contains "canister"
    RefinedGoods,     // any other RefinedProduct item
    Crystal,          // ItemCategory.Crystal
    TradeGoods,       // ItemCategory.TradeGoods
    Salvage,          // ItemCategory.Salvage
    Other,            // ItemCategory.Junk / anything else recognised
}
