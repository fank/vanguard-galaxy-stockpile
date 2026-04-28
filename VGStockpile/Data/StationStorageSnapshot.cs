using System.Collections.Generic;

namespace VGStockpile.Data;

internal sealed record StationStorageSnapshot(
    string StationId,
    string StationName,
    string SystemName,
    string FactionId,
    IReadOnlyDictionary<string, int> Items);
