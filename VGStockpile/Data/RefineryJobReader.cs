using System.Collections.Generic;
using BepInEx.Logging;
using Source.Galaxy;
using Source.Galaxy.POI;
using VGStockpile.UI.Refinery;

namespace VGStockpile.Data;

internal sealed class RefineryJobReader
{
    private readonly ManualLogSource _log;

    public RefineryJobReader(ManualLogSource log) { _log = log; }

    public IReadOnlyList<RefineryJobSnapshot> CaptureAll()
    {
        var data = GalaxyMapData.current;
        if (data is null)
        {
            _log.LogWarning("GalaxyMapData.current is null; returning empty refinery-job list.");
            return System.Array.Empty<RefineryJobSnapshot>();
        }

        var result = new List<RefineryJobSnapshot>();

        foreach (var poi in data.allPointsOfInterest)
        {
            if (poi is not SpaceStation st) continue;
            var refinery = st.refinery;
            if (refinery?.jobs is null) continue;

            foreach (var job in refinery.jobs)
            {
                if (job?.ore?.item is null) continue;

                var id = job.ore.item.identifier;
                if (string.IsNullOrEmpty(id)) continue;

                // `progress` is publicized in the compile-time stub but ships
                // private at runtime (Mono throws FieldAccessException on read).
                // Reconstruct it from the public jobProgress property
                // (jobProgress == progress / refineTime).
                var refineTime = job.refineTime;
                var progress   = job.jobProgress * refineTime;

                result.Add(new RefineryJobSnapshot(
                    StationId:        st.guid ?? "",
                    StationName:      st.name ?? "",
                    SystemGuid:       st.system?.guid ?? "",
                    SystemName:       st.system?.name ?? "",
                    FactionId:        st.faction?.identifier ?? "",
                    MaterialId:       id,
                    ProgressFraction: RefineryMath.ProgressFraction(
                                          job.initialAmount, job.remainingAmount,
                                          progress, refineTime),
                    RemainingAmount:  job.remainingAmount,
                    InitialAmount:    job.initialAmount,
                    EtaSeconds:       RefineryMath.EtaSeconds(
                                          progress, refineTime, job.remainingAmount),
                    MaxJobs:          refinery.maxJobs));
            }
        }

        return result;
    }
}
