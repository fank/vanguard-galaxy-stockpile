using System.Collections.Generic;
using BepInEx.Logging;
using Source.Galaxy;
using Source.Galaxy.POI;
using Behaviour.Item;
using Source.Item;

namespace VGStockpile.Transfers.Engine;

internal sealed class MaterialStorageMutator : IMaterialStorageMutator
{
    private readonly ManualLogSource _log;

    public MaterialStorageMutator(ManualLogSource log) { _log = log; }

    public void Reserve(string sourceGuid, IReadOnlyList<TransferManifestLine> manifest) =>
        Subtract(sourceGuid, manifest, "Reserve");

    public void Deliver(string destGuid, IReadOnlyList<TransferManifestLine> manifest) =>
        AddAll(destGuid, manifest, "Deliver");

    public void Return(string sourceGuid, IReadOnlyList<TransferManifestLine> manifest) =>
        AddAll(sourceGuid, manifest, "Return");

    private void Subtract(string guid, IReadOnlyList<TransferManifestLine> manifest, string op)
    {
        var inv = ResolveStorage(guid, op); if (inv is null) return;
        for (var i = 0; i < manifest.Count; i++)
        {
            var line = manifest[i];
            if (!InventoryItemType.TryGet(line.ItemIdentifier, out var type))
            {
                _log.LogError($"{op}: unknown item identifier '{line.ItemIdentifier}' at {guid}.");
                continue;
            }
            var removed = inv.Remove(type, line.Quantity);
            if (removed < line.Quantity)
                _log.LogWarning(
                    $"{op}: only removed {removed}/{line.Quantity} of {line.ItemIdentifier} from {guid}.");
        }
    }

    private void AddAll(string guid, IReadOnlyList<TransferManifestLine> manifest, string op)
    {
        var inv = ResolveStorage(guid, op); if (inv is null) return;
        for (var i = 0; i < manifest.Count; i++)
        {
            var line = manifest[i];
            if (!InventoryItemType.TryGet(line.ItemIdentifier, out var type))
            {
                _log.LogError($"{op}: unknown item identifier '{line.ItemIdentifier}' at {guid} — items lost.");
                continue;
            }
            inv.Add(type, line.Quantity);
        }
    }

    private Inventory? ResolveStorage(string stationGuid, string op)
    {
        var data = GalaxyMapData.current;
        if (data is null) { _log.LogError($"{op}: GalaxyMapData.current is null."); return null; }

        foreach (var poi in data.allPointsOfInterest)
        {
            if (poi is not SpaceStation st) continue;
            if (st.guid != stationGuid) continue;
            if (st.materialStorage is null)
            {
                _log.LogError($"{op}: station {stationGuid} has null materialStorage.");
                return null;
            }
            return st.materialStorage;
        }
        _log.LogError($"{op}: station {stationGuid} not found.");
        return null;
    }
}
