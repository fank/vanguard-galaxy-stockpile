namespace VGStockpile.Data;

internal interface IMaterialCatalog
{
    string DisplayName(string materialTypeId);

    MaterialCategory Category(string materialTypeId);

    /// <summary>
    /// Vanilla "Sort by Type" mirrors the InventoryItemType triple
    /// (itemCategory, gameplayType, name). These three accessors expose
    /// each leg of that key. Unknown ids return int.MaxValue / "" so they
    /// sort last/together.
    /// </summary>
    int CategoryOrder(string materialTypeId);

    /// <summary>GameplayType enum value (Combat / Generic / …).</summary>
    int GameplayTypeOrder(string materialTypeId);

    /// <summary>The InventoryItemType.name (Unity Object name) used as
    /// the tertiary sort key by vanilla.</summary>
    string SortName(string materialTypeId);
}
