namespace VGStockpile.Data;

internal interface IMaterialCatalog
{
    string DisplayName(string materialTypeId);

    MaterialCategory Category(string materialTypeId);
}
