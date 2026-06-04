using System.Collections.Generic;
using System.Linq;
using VGStockpile.Data;

namespace VGStockpile.UI.Transfers;

/// <summary>
/// Resolves which station's stock a transfer dialog should offer as its source,
/// read from the <em>live</em> snapshot list rather than the (possibly stale)
/// clicked-row snapshot captured when the stockpile window was opened.
/// Pull's source is the clicked remote station; Push's source is the docked
/// station. Returns empty when that source has no live snapshot (e.g. it was
/// drained to empty and dropped by the reader) or no docked station is known —
/// never the stale snapshot, so the dialog can't offer ore that already left.
/// </summary>
internal static class TransferSourceStock
{
    private static readonly IReadOnlyDictionary<string, int> Empty =
        new Dictionary<string, int>();

    public static IReadOnlyDictionary<string, int> Resolve(
        TransferDirection dir,
        StationStorageSnapshot clickedRow,
        string? dockedStationGuid,
        IReadOnlyList<StationStorageSnapshot> liveSnapshots)
    {
        var sourceGuid = dir == TransferDirection.Pull
            ? clickedRow.StationId
            : dockedStationGuid;
        if (string.IsNullOrEmpty(sourceGuid)) return Empty;
        return liveSnapshots.FirstOrDefault(s => s.StationId == sourceGuid)?.Items ?? Empty;
    }
}
