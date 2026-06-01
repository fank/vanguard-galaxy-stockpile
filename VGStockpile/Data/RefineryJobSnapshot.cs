namespace VGStockpile.Data;

/// <summary>
/// One active refinery job at a station, captured for display. Pure data — the
/// material name/icon resolve via <see cref="IMaterialCatalog"/> from
/// <see cref="MaterialId"/>, the same path the storage grid uses.
/// </summary>
internal sealed record RefineryJobSnapshot(
    string StationId,
    string StationName,
    string SystemGuid,
    string SystemName,
    string FactionId,
    string MaterialId,
    float  ProgressFraction,   // 0..1 overall job completion
    int    RemainingAmount,
    int    InitialAmount,
    float  EtaSeconds,
    int    MaxJobs);           // the station's available queue slots
