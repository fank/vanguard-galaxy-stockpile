using BepInEx.Logging;
using Source.Galaxy.POI;

namespace VGStockpile.Transfers.Engine;

internal sealed class StationContextAdapter
{
    private readonly ManualLogSource _log;

    public StationContextAdapter(ManualLogSource log) { _log = log; }

    /// <summary>
    /// Builds a <see cref="StationContext"/> for the eligibility rules from a
    /// vanilla <see cref="SpaceStation"/>. <paramref name="current"/> is the
    /// station the player is currently docked at (or null if not docked).
    /// </summary>
    public StationContext FromStation(SpaceStation st, SpaceStation? current)
    {
        return new StationContext(
            Guid: st.guid ?? "",
            HasBeenVisited: st.lastVisitedTime > 0f,
            HasAnyStoredMaterials: HasAnyStoredMaterials(st),
            HasRefinery: st.HasFacility(SpaceStationFacility.Refinery),
            IsPeaceful: st.PlayerIsFriendly(),
            IsCurrent: current is not null && current.guid == st.guid,
            IsPlayerDocked: current is not null);
    }

    private static bool HasAnyStoredMaterials(SpaceStation st)
    {
        var inv = st.materialStorage; if (inv is null) return false;
        foreach (var slot in inv.items)
            if (slot?.item is not null && slot.count > 0) return true;
        return false;
    }
}
