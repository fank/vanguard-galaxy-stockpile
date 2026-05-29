using System.Collections;
using System.Reflection;
using BepInEx.Logging;
using Behaviour.UI.Side_Menu;
using HarmonyLib;
using Source.Galaxy;
using Source.Galaxy.POI;
using VGStockpile.Data;

namespace VGStockpile.Locate;

internal sealed class StationLocator : IStationLocator
{
    private readonly ManualLogSource _log;

    // The publicizer stub marks SidePanel.OpenMapAndFocusPoi as public, but the
    // runtime DLL still ships it as private (or internal) — Mono throws
    // MethodAccessException on direct invocation. Resolve via reflection once
    // and cache.
    private static MethodInfo? _openMapAndFocusPoi;
    private static MethodInfo OpenMapAndFocusPoiMethod =>
        _openMapAndFocusPoi ??= AccessTools.Method(
            typeof(SidePanel),
            nameof(SidePanel.OpenMapAndFocusPoi));

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

        FocusStation(sidePanel, station);
    }

    public void LocateByGuid(string stationGuid)
    {
        if (string.IsNullOrEmpty(stationGuid)) return;
        var sidePanel = SidePanel.instance;
        if (sidePanel is null)
        {
            _log.LogWarning("LocateByGuid: SidePanel not initialized.");
            return;
        }
        var data = GalaxyMapData.current;
        if (data is null) { _log.LogWarning("LocateByGuid: GalaxyMapData.current is null."); return; }
        foreach (var poi in data.allPointsOfInterest)
        {
            if (poi is not SpaceStation st) continue;
            if (st.guid != stationGuid) continue;
            FocusStation(sidePanel, st);
            return;
        }
        _log.LogWarning($"LocateByGuid: station guid '{stationGuid}' not found in GalaxyMapData.");
    }

    private void FocusStation(SidePanel sidePanel, SpaceStation station)
    {
        var method = OpenMapAndFocusPoiMethod;
        if (method is null)
        {
            _log.LogError(
                "AccessTools could not resolve SidePanel.OpenMapAndFocusPoi; " +
                "vanilla likely renamed it. Falling back to no-op.");
            return;
        }

        // Invoke the private coroutine factory, then run the returned
        // IEnumerator on the SidePanel MonoBehaviour.
        var routine = method.Invoke(sidePanel,
            new object[] { station, /* waitForClose */ false }) as IEnumerator;
        if (routine is null)
        {
            _log.LogError("OpenMapAndFocusPoi returned non-IEnumerator; cannot start coroutine.");
            return;
        }
        sidePanel.StartCoroutine(routine);
    }
}
