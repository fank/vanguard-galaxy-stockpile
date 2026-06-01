using System.Collections.Generic;

namespace VGStockpile.Data;

internal sealed record StationStorageSnapshot(
    string StationId,
    string StationName,
    string SystemGuid,
    string SystemName,
    string FactionId,
    IReadOnlyDictionary<string, int> Items,
    // null = station has no refinery; true/false = auto-refine on/off.
    bool? AutoRefine = null);
