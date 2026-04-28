using VGStockpile.Data;

namespace VGStockpile.Locate;

internal interface IStationLocator
{
    void Locate(StationStorageSnapshot snapshot);
}
