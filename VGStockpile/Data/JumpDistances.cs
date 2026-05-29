using System.Collections.Generic;
using Source.Galaxy;

namespace VGStockpile.Data;

internal static class JumpDistances
{
    /// <summary>
    /// Returns systemGuid → jump count from the player's current system.
    /// Thin wrapper over <see cref="ComputeFrom"/> using
    /// <see cref="SystemMapData.current"/> as the start node.
    /// </summary>
    internal static IReadOnlyDictionary<string, int> ComputeFromCurrent()
        => ComputeFrom(SystemMapData.current);

    /// <summary>
    /// Returns systemGuid → jump count from <paramref name="start"/>.
    /// Computed via BFS over <see cref="SystemMapData.GetAdjacentSystems"/>.
    /// <paramref name="start"/> reports 0; unreachable systems are absent from
    /// the returned dictionary. Returns an empty dictionary when
    /// <paramref name="start"/> is <see langword="null"/>.
    /// </summary>
    internal static IReadOnlyDictionary<string, int> ComputeFrom(SystemMapData? start)
    {
        var distances = new Dictionary<string, int>();
        if (start is null) return distances;

        distances[start.guid] = 0;
        var queue = new Queue<SystemMapData>();
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            var d = distances[node.guid];
            foreach (var nb in node.GetAdjacentSystems())
            {
                if (nb is null) continue;
                var id = nb.guid;
                if (string.IsNullOrEmpty(id)) continue;
                if (distances.ContainsKey(id)) continue;
                distances[id] = d + 1;
                queue.Enqueue(nb);
            }
        }
        return distances;
    }
}
