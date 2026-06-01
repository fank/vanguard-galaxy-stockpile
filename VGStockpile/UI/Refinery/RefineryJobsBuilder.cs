using System;
using System.Collections.Generic;
using System.Linq;
using VGStockpile.Data;

namespace VGStockpile.UI.Refinery;

internal readonly record struct RefineryJobRow(
    RefineryJobSnapshot Snapshot,
    string MaterialName);

internal readonly record struct RefineryStationGroup(
    string StationId,
    string StationName,
    string SystemName,
    string FactionId,
    int MaxJobs,
    IReadOnlyList<RefineryJobRow> Jobs);

/// <summary>
/// Groups refinery-job snapshots by station (so the station name shows once as
/// a header instead of on every row) and resolves each material's localized
/// display name. Stations and the jobs within them sort by name — a stable
/// order that doesn't reshuffle as ETAs tick down. Pure — unit-testable with a
/// fake catalog.
/// </summary>
internal sealed class RefineryJobsBuilder
{
    private readonly IMaterialCatalog _catalog;

    public RefineryJobsBuilder(IMaterialCatalog catalog) { _catalog = catalog; }

    public IReadOnlyList<RefineryStationGroup> Build(IReadOnlyList<RefineryJobSnapshot> snapshots)
    {
        return snapshots
            .Select(s => new RefineryJobRow(s, _catalog.DisplayName(s.MaterialId)))
            .GroupBy(r => r.Snapshot.StationId)
            .Select(g =>
            {
                var first = g.First().Snapshot;
                return new RefineryStationGroup(
                    g.Key,
                    first.StationName,
                    first.SystemName,
                    first.FactionId,
                    first.MaxJobs,
                    g.OrderBy(r => r.MaterialName, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(r => r.Snapshot.MaterialId, StringComparer.Ordinal)
                     .ToArray());
            })
            .OrderBy(grp => grp.StationName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(grp => grp.StationId, StringComparer.Ordinal)
            .ToArray();
    }
}
