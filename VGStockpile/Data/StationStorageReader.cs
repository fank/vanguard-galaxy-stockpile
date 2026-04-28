using System.Collections.Generic;
using BepInEx.Logging;
using Source.Galaxy;
using Source.Galaxy.POI;
using Source.Item;

namespace VGStockpile.Data;

internal sealed class StationStorageReader
{
    private readonly ManualLogSource _log;

    public StationStorageReader(ManualLogSource log) { _log = log; }

    public IReadOnlyList<StationStorageSnapshot> CaptureAll()
    {
        var data = GalaxyMapData.current;
        if (data is null)
        {
            _log.LogWarning("GalaxyMapData.current is null; returning empty snapshot list.");
            return System.Array.Empty<StationStorageSnapshot>();
        }

        var result = new List<StationStorageSnapshot>();

        foreach (var poi in data.allPointsOfInterest)
        {
            if (poi is not SpaceStation st) continue;
            var inv = st.materialStorage;
            if (inv is null) continue;

            var items = ReadItems(inv);
            if (items.Count == 0) continue;

            result.Add(new StationStorageSnapshot(
                StationId:   st.guid ?? "",
                StationName: st.name ?? "",
                SystemName:  st.system?.name ?? "",
                FactionId:   st.faction?.identifier ?? "",
                Items:       items));
        }

        return result;
    }

    private static IReadOnlyDictionary<string, int> ReadItems(Inventory inv)
    {
        // `Inventory.items` enumerates non-null InventoryItem instances.
        // Multiple slots can hold the same type, so sum across slots.
        var dict = new Dictionary<string, int>();
        foreach (var slot in inv.items)
        {
            if (slot?.item is null) continue;
            var id  = slot.item.identifier;
            var qty = slot.count;
            if (qty <= 0 || string.IsNullOrEmpty(id)) continue;
            dict[id] = dict.TryGetValue(id, out var existing) ? existing + qty : qty;
        }
        return dict;
    }
}
