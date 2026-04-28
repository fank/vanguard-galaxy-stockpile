using System;
using VGStockpile.Data;
using VGStockpile.Locate;

namespace VGStockpile.UI;

internal sealed class StationRowClickHandler
{
    private readonly IStationLocator _locator;
    private readonly Action          _closeWindow;
    private readonly Func<bool>      _shouldCloseOnLocate;
    private readonly Action<string>  _logWarning;

    public StationRowClickHandler(
        IStationLocator locator,
        Action closeWindow,
        Func<bool> shouldCloseOnLocate,
        Action<string> logWarning)
    {
        _locator = locator;
        _closeWindow = closeWindow;
        _shouldCloseOnLocate = shouldCloseOnLocate;
        _logWarning = logWarning;
    }

    public void Click(StationStorageSnapshot snapshot)
    {
        try
        {
            _locator.Locate(snapshot);
        }
        catch (Exception ex)
        {
            _logWarning($"Locate failed for station {snapshot.StationName}: {ex.Message}");
            return;
        }

        if (_shouldCloseOnLocate()) _closeWindow();
    }
}
