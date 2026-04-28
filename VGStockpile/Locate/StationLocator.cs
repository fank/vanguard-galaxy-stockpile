using BepInEx.Logging;
using Behaviour.UI.Side_Menu;
using Source.Galaxy;
using Source.Galaxy.POI;
using VGStockpile.Data;

namespace VGStockpile.Locate;

internal sealed class StationLocator : IStationLocator
{
    private readonly ManualLogSource _log;

    public StationLocator(ManualLogSource log) { _log = log; }

    public void Locate(StationStorageSnapshot snapshot)
    {
        var sidePanel = SidePanel.instance;
        if (sidePanel is null)
        {
            _log.LogWarning(
                $"SidePanel not initialized; cannot locate {snapshot.StationName}.");
            return;
        }

        var data = GalaxyMapData.current;
        var poi  = data?.GetPointOfInterest(snapshot.StationId);
        if (poi is not SpaceStation station)
        {
            _log.LogWarning(
                $"Station {snapshot.StationName} ({snapshot.StationId}) not found in GalaxyMapData.");
            return;
        }

        // Vanilla "Locate" path — same coroutine the mission UI uses.
        sidePanel.StartCoroutine(sidePanel.OpenMapAndFocusPoi(station, waitForClose: false));
    }
}
