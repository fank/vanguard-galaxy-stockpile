using BepInEx.Logging;
using Behaviour.GalaxyMap;
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
        var mgr = AbstractGalaxyMapManager.current;
        if (mgr is null)
        {
            _log.LogWarning(
                $"GalaxyMapManager not initialized; cannot locate {snapshot.StationName}.");
            return;
        }

        // Resolve the live SpaceStation POI by stable id. Snapshot is a flat
        // record so we re-look-up every time (cheap; happens on click only).
        var data = GalaxyMapData.current;
        var poi  = data?.GetPointOfInterest(snapshot.StationId);
        if (poi is not SpaceStation station)
        {
            _log.LogWarning(
                $"Station {snapshot.StationName} ({snapshot.StationId}) not found in GalaxyMapData.");
            return;
        }

        // Best-guess sequence. Vanilla "Locate" exact chain is throw-stubbed
        // in the publicized DLL; this mirrors the available public API:
        //   1. Switch the map to the station's home system.
        //   2. Mark the station as the current map focus.
        //   3. Make sure the map window is visible.
        if (station.system is SystemMapData sys)
        {
            mgr.ShowSystemMap(sys);
        }
        mgr.focusPointOfInterest = station;

        if (mgr.mapWindow != null && !mgr.mapWindow.gameObject.activeSelf)
        {
            mgr.mapWindow.gameObject.SetActive(true);
        }
    }
}
